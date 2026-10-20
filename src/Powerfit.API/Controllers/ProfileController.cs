using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Powerfit.API.Extensions;
using Powerfit.Business.DTOs.Profiles;
using Powerfit.Business.Interfaces;

namespace Powerfit.API.Controllers;

[ApiController]
[Authorize(Policy = AuthPolicies.UserOnly)]
[Route("api/profile")]
public class ProfileController(IProfileService profiles) : ControllerBase
{
    /// <summary>The signed-in user's profile (empty profile when nothing was saved yet).</summary>
    [HttpGet]
    public async Task<ActionResult<ProfileDto>> Get(CancellationToken ct) =>
        Ok(await profiles.GetAsync(User.GetUserId(), ct));

    /// <summary>Creates or replaces the profile. Fields left null are cleared.</summary>
    [HttpPut]
    public async Task<ActionResult<ProfileDto>> Update(UpdateProfileRequest request, CancellationToken ct) =>
        Ok(await profiles.UpdateAsync(User.GetUserId(), request, ct));
}
