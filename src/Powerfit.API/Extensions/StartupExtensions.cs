using Powerfit.Business.DTOs.Auth;
using Powerfit.Business.Interfaces;

namespace Powerfit.API.Extensions;

public static class StartupExtensions
{
    /// <summary>
    /// Run once before app.Run(): logs the database status and creates the first admin when none exists.
    /// Reads AdminSeed:Email / Password / FirstName / LastName (skipped when Email or Password is empty).
    /// Remove AdminSeed:Password from the configuration after the first run.
    /// Goes through the business tier only (IAppInitializer): the API no longer references the data tier.
    /// </summary>
    public static async Task InitializeAsync(this WebApplication app, CancellationToken ct = default)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
        var initializer = scope.ServiceProvider.GetRequiredService<IAppInitializer>();

        if (string.IsNullOrWhiteSpace(app.Configuration["Ai:ApiKey"]))
            logger.LogWarning("Ai:ApiKey is not configured: plan generation will fail until it is set.");

        try
        {
            if (await initializer.CanConnectAsync(ct))
                logger.LogInformation("Database connection successful.");
            else
                logger.LogError("Database connection failed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database connection failed.");
        }

        var section = app.Configuration.GetSection("AdminSeed");
        var email = section["Email"];
        var password = section["Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("AdminSeed is not configured: no admin will be created.");
            return;
        }

        try
        {
            var created = await initializer.EnsureFirstAdminAsync(new RegisterRequest
            {
                FirstName = section["FirstName"] ?? "Admin",
                LastName = section["LastName"] ?? "PowerFit",
                Email = email,
                Password = password
            }, ct);

            if (created)
                logger.LogWarning(
                    "The first admin '{Email}' was created. Remove AdminSeed:Password from the configuration now.", email);
        }
        catch (Exception ex)
        {
            // Do not stop the whole API if the database is not reachable yet.
            logger.LogError(ex, "Admin seeding failed.");
        }
    }
}
