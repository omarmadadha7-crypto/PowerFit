using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataAccess.Entities;
using DataAccess.Repositories;
using Powerfit.Business.DTOs.Auth;
using Powerfit.Business.Exceptions;
using Powerfit.Business.Interfaces;
using Powerfit.Business.Mapping;
using Powerfit.Business.Validators;

namespace Powerfit.Business.Services;

public class AuthService(
    IUnitOfWork uow,
    IPasswordHasher hasher,
    ITokenService tokens,
    ILoginAttemptTracker attempts) : IAuthService
{
    // Verified against when the e-mail is unknown, so "no such account" costs the same time as "wrong password".
    private static string? _dummyHash;
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        AuthValidator.Validate(request);
        await EnsureEmailIsFreeAsync(request.Email, ct);

        var person = Person.CreateUser(
            request.FirstName, request.LastName, request.Email,
            hasher.Hash(request.Password), request.BirthDate);

        await uow.People.AddAsync(person, ct);
        await SavePersonAsync(person, ct);

        var user = person.Users.First();
        var token = tokens.CreateToken(new TokenSubject(
            person.PersonId, user.UserId, person.Email, person.FullName, person.Role));

        return new AuthResponse(token.Token, token.ExpiresAtUtc, person.ToAuthUserDto(user.UserId, null, false));
    }

    public async Task<AuthUserDto> CreateAdminAsync(RegisterRequest request, CancellationToken ct = default)
    {
        AuthValidator.Validate(request);
        await EnsureEmailIsFreeAsync(request.Email, ct);

        var person = Person.CreateAdmin(
            request.FirstName, request.LastName, request.Email,
            hasher.Hash(request.Password), request.BirthDate);

        await uow.People.AddAsync(person, ct);
        await SavePersonAsync(person, ct);

        var admin = person.Admins.First();
        return person.ToAuthUserDto(null, admin.AdminId, false);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        AuthValidator.Validate(request);

        var key = request.Email.Trim().ToLowerInvariant();
        if (attempts.IsLockedOut(key, out var retryAfter))
            throw new TooManyRequestsException("Too many failed sign-in attempts. Please try again later.", retryAfter);

        var person = await uow.People.GetByEmailAsync(request.Email, ct);

        // Always run exactly one password verification, even for unknown e-mails (timing side channel).
        var passwordOk = hasher.Verify(request.Password, person?.PasswordHash ?? DummyHash());
        if (person is null || !passwordOk)
        {
            attempts.RecordFailure(key);
            throw new UnauthorizedException("Invalid email or password.");
        }

        attempts.Reset(key);

        int? userId = null;
        int? adminId = null;
        var profileCompleted = false;

        if (person.Role == UserRoles.Admin)
        {
            var admin = await uow.Admins.GetByPersonIdAsync(person.PersonId, ct)
                ?? throw new UnauthorizedException("This account has no admin record.");
            adminId = admin.AdminId;
        }
        else
        {
            var user = await uow.Users.GetByPersonIdAsync(person.PersonId, ct)
                ?? throw new UnauthorizedException("This account has no user record.");
            userId = user.UserId;
            profileCompleted = await uow.Profiles.IsCompletedAsync(user.UserId, ct);
        }

        // Upgrade old hashes silently (e.g. when the iteration count is raised).
        if (hasher.NeedsRehash(person.PasswordHash))
        {
            person.PasswordHash = hasher.Hash(request.Password);
            await uow.SaveChangesAsync(ct);
        }

        var accountId = adminId ?? userId!.Value;
        var token = tokens.CreateToken(new TokenSubject(
            person.PersonId, accountId, person.Email, person.FullName, person.Role));

        return new AuthResponse(token.Token, token.ExpiresAtUtc, person.ToAuthUserDto(userId, adminId, profileCompleted));
    }

    public async Task ChangePasswordAsync(int personId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        AuthValidator.Validate(request);

        var person = await uow.People.GetByIdAsync(personId, ct)
            ?? throw new NotFoundException("Person", personId);

        if (!hasher.Verify(request.CurrentPassword, person.PasswordHash))
            throw new ValidationException("currentPassword", "Current password is incorrect.");

        if (request.CurrentPassword == request.NewPassword)
            throw new ValidationException("newPassword", "The new password must be different from the current one.");

        person.PasswordHash = hasher.Hash(request.NewPassword);
        await uow.SaveChangesAsync(ct);
    }

    // ------------------------------------------------------------------------

    private string DummyHash() =>
        LazyInitializer.EnsureInitialized(ref _dummyHash, () => hasher.Hash(Guid.NewGuid().ToString("N")))!;

    private async Task EnsureEmailIsFreeAsync(string email, CancellationToken ct)
    {
        if (await uow.People.EmailExistsAsync(email, null, ct))
            throw new ConflictException("An account with this email already exists.");
    }

    private async Task SavePersonAsync(Person person, CancellationToken ct)
    {
        try
        {
            await uow.SaveChangesAsync(ct);
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            // Most likely a concurrent request inserted the same e-mail (UQ_Person_Email).
            var email = person.Email;
            uow.ClearChangeTracker();

            if (await uow.People.EmailExistsAsync(email, null, ct))
                throw new ConflictException("An account with this email already exists.");

            throw;
        }
    }
}
