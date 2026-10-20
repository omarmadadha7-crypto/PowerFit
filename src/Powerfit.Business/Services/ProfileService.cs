using System.Threading;
using System.Threading.Tasks;
using DataAccess.Repositories;
using Powerfit.Business.DTOs.Profiles;
using Powerfit.Business.Exceptions;
using Powerfit.Business.Interfaces;
using Powerfit.Business.Mapping;
using Powerfit.Business.Validators;

namespace Powerfit.Business.Services;

public class ProfileService(IUnitOfWork uow) : IProfileService
{
    public async Task<ProfileDto> GetAsync(int userId, CancellationToken ct = default)
    {
        var user = await uow.Users.GetWithPersonAndProfileAsync(userId, ct)
            ?? throw new NotFoundException("User", userId);

        return user.Profile is { } profile
            ? profile.ToDto(user.Person)
            : ProfileMappings.EmptyProfile(userId, user.Person);
    }

    public async Task<ProfileDto> UpdateAsync(int userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        ProfileValidator.Validate(request);

        var user = await uow.Users.GetWithPersonAndProfileAsync(userId, ct)
            ?? throw new NotFoundException("User", userId);

        // Creates the row if it does not exist, otherwise copies the fields and refreshes ProfileCompleted.
        var profile = await uow.Profiles.UpsertAsync(userId, request.ToEntity(), ct);

        user.Person.BirthDate = request.BirthDate;

        await uow.SaveChangesAsync(ct);
        return profile.ToDto(user.Person);
    }
}
