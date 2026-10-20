using System.Threading;
using System.Threading.Tasks;
using Powerfit.Business.DTOs.Auth;

namespace Powerfit.Business.Interfaces;

public interface IAuthService
{
    /// <summary>Creates a normal user account and signs it in.</summary>
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);

    /// <summary>Creates an admin account. Call it from an admin-only endpoint (or a seeding tool).</summary>
    Task<AuthUserDto> CreateAdminAsync(RegisterRequest request, CancellationToken ct = default);

    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);

    Task ChangePasswordAsync(int personId, ChangePasswordRequest request, CancellationToken ct = default);
}
