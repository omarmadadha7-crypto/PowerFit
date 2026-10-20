using Powerfit.API.Extensions;
using Powerfit.API.Middlewares;
using Powerfit.Business;

namespace Powerfit;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ------------------------------------------------------------------ services
        builder.Services
            .AddControllers()
            .AddUniformValidationErrors();          // model-binding errors use the same ErrorResponse shape

        builder.Services.AddBusiness(builder.Configuration);   // data tier (DbContext, repositories) + business tier
        builder.Services.AddJwtAuthentication(builder.Configuration);
        builder.Services.AddPowerFitRateLimiting();
        builder.Services.AddPowerFitRequestTimeouts(builder.Configuration);
        builder.Services.AddPowerFitCors(builder.Configuration, builder.Environment);
        builder.Services.AddPowerFitForwardedHeaders(builder.Configuration);
        builder.Services.AddSwaggerDocs();

        var app = builder.Build();

        // ------------------------------------------------------------------ pipeline (order matters)
        app.UseMiddleware<ExceptionHandlingMiddleware>();   // first: catches everything below
        app.UseForwardedHeaders();                          // no-op unless ForwardedHeaders:TrustedProxies is set

        if (!app.Environment.IsDevelopment())
            app.UseHsts();

        app.UseHttpsRedirection();

        if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
            app.UseSwaggerDocs();

        app.UseCors(HostingExtensions.CorsPolicy);          // before authentication (answers preflight requests)
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();                               // after authentication: the AI limiter keys on the "uid" claim
        app.UseRequestTimeouts();                           // needed by [RequestTimeout("AiGeneration")]
        app.MapControllers();

        // ------------------------------------------------------------------ start-up work
        await app.InitializeAsync();                        // DB check + first admin (AdminSeed section)

        await app.RunAsync();
    }
}
