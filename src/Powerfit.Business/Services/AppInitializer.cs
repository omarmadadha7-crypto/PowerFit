using System.Threading;
using System.Threading.Tasks;
using DataAccess.Repositories;
using Powerfit.Business.DTOs.Auth;
using Powerfit.Business.Interfaces;

namespace Powerfit.Business.Services;

public class AppInitializer(IUnitOfWork uow, IAuthService auth) : IAppInitializer
{
    public Task<bool> CanConnectAsync(CancellationToken ct = default) => uow.CanConnectAsync(ct);

    public async Task<bool> EnsureFirstAdminAsync(RegisterRequest admin, CancellationToken ct = default)
    {
        if (await uow.Admins.CountAsync(null, ct) > 0)
            return false;

        await auth.CreateAdminAsync(admin, ct);
        return true;
    }
}
