using System;
using DataAccess;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Powerfit.Business.AI;
using Powerfit.Business.Interfaces;
using Powerfit.Business.Security;
using Powerfit.Business.Services;
using Powerfit.Business.Settings;

namespace Powerfit.Business;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the whole backend: the data tier (DbContext + repositories, via AddDataAccess) and the business tier.
    /// The API host only needs this one call. Reads "ConnectionStrings:DefaultConnection", "Jwt" and "Ai".
    /// </summary>
    public static IServiceCollection AddBusiness(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDataAccess(configuration);

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<AiSettings>(configuration.GetSection(AiSettings.SectionName));

        // Stateless helpers
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, TokenService>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<ILoginAttemptTracker, LoginAttemptTracker>();

        // AI generator: typed HttpClient (timeout + base address come from AiSettings)
        services.AddHttpClient<IAiPlanGenerator, AiPlanGenerator>((sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<AiSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(Math.Max(10, settings.TimeoutSeconds));
        });

        // Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IPlanService, PlanService>();
        services.AddScoped<IPlanEditingService, PlanEditingService>();
        services.AddScoped<IExerciseService, ExerciseService>();
        services.AddScoped<IFoodService, FoodService>();
        services.AddScoped<IAppInitializer, AppInitializer>();

        return services;
    }
}
