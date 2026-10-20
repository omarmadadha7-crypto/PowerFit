using System.Threading;
using System.Threading.Tasks;
using Powerfit.Business.DTOs.Profiles;

namespace Powerfit.Business.Interfaces;

public interface IProfileService
{
    /// <summary>Returns an empty profile (ProfileCompleted = false) when the user has not saved one yet.</summary>
    Task<ProfileDto> GetAsync(int userId, CancellationToken ct = default);

    /// <summary>Creates or replaces the profile (and the birth date stored on Person).</summary>
    Task<ProfileDto> UpdateAsync(int userId, UpdateProfileRequest request, CancellationToken ct = default);
}
