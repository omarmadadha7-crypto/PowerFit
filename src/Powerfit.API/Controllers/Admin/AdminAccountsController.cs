using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Powerfit.API.Extensions;
using Powerfit.Business.DTOs.Auth;
using Powerfit.Business.Interfaces;

namespace Powerfit.API.Controllers.Admin;

[ApiController]
[Authorize(Policy = AuthPolicies.AdminOnly)]
[Route("api/admin/admins")]
public class AdminAccountsController(IAuthService auth) : ControllerBase
{
    /// <summary>Creates another admin account (the first admin comes from AdminSeed in appsettings).</summary>
    [HttpPost]
    public async Task<ActionResult<AuthUserDto>> CreateAdmin(RegisterRequest request, CancellationToken ct)
    {
        var admin = await auth.CreateAdminAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, admin);
    }
}
