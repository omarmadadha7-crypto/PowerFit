using System.Threading;
using System.Threading.Tasks;
using Powerfit.Business.DTOs.Auth;

namespace Powerfit.Business.Interfaces;

/// <summary>Start-up work the API host needs, exposed so the API never touches the data tier directly.</summary>
public interface IAppInitializer
{
    Task<bool> CanConnectAsync(CancellationToken ct = default);

    /// <summary>Creates the first admin when there is none. Returns true when an admin was created.</summary>
    Task<bool> EnsureFirstAdminAsync(RegisterRequest admin, CancellationToken ct = default);
}
