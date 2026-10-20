using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Powerfit.API.Responses;
using Powerfit.Business.Settings;

namespace Powerfit.API.Extensions;

/// <summary>Host wiring that used to be missing from Program.cs: error shape, CORS, timeouts, proxy headers.</summary>
public static class HostingExtensions
{
    public const string CorsPolicy = "Frontend";

    /// <summary>Name used by [RequestTimeout("AiGeneration")] on PlansController.Generate.</summary>
    public const string AiGenerationTimeoutPolicy = "AiGeneration";

    /// <summary>
    /// [ApiController] answers a bad JSON body or a missing "required" property with ValidationProblemDetails,
    /// which breaks the "one error shape for the whole API" promise. This makes it use ErrorResponse (400).
    /// </summary>
    public static IMvcBuilder AddUniformValidationErrors(this IMvcBuilder mvc) =>
        mvc.ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

                foreach (var (key, entry) in context.ModelState)
                {
                    if (entry.Errors.Count == 0) continue;

                    var field = key.TrimStart('$', '.');
                    if (field.Length == 0) field = "request";

                    if (!errors.TryGetValue(field, out var messages))
                        errors[field] = messages = new List<string>();

                    foreach (var error in entry.Errors)
                    {
                        // Deserialisation errors carry framework internals (type names, line numbers): hide them.
                        messages.Add(error.Exception is not null || string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? field == "request"
                                ? "The request body is missing or is not valid JSON."
                                : "The value is missing or has the wrong type."
                            : error.ErrorMessage);
                    }
                }

                var body = ErrorResponse.From(
                    context.HttpContext,
                    StatusCodes.Status400BadRequest,
                    "Validation failed",
                    "One or more validation errors occurred.",
                    errors.ToDictionary(e => e.Key, e => e.Value.Distinct().ToArray()));

                return new ObjectResult(body) { StatusCode = StatusCodes.Status400BadRequest };
            };
        });

    /// <summary>
    /// Allowed browser origins come from "Cors:AllowedOrigins" (array). Development with nothing configured allows any
    /// origin; outside Development an empty list means no cross-origin access at all.
    /// </summary>
    public static IServiceCollection AddPowerFitCors(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var origins = (configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .Select(o => o.Trim().TrimEnd('/'))
            .ToArray();

        services.AddCors(options => options.AddPolicy(CorsPolicy, policy =>
        {
            if (origins.Length > 0)
                policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Retry-After");
            else if (environment.IsDevelopment())
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Retry-After");
            // else: no origin is allowed. The JWT travels in a header, so AllowCredentials is not needed.
        }));

        return services;
    }

    /// <summary>
    /// The plan-generation request may take as long as the provider timeout (Ai:TimeoutSeconds) plus the
    /// database work around it. A shorter budget would cut the request before the 504 mapping can answer.
    /// </summary>
    public static IServiceCollection AddPowerFitRequestTimeouts(this IServiceCollection services, IConfiguration configuration)
    {
        var ai = configuration.GetSection(AiSettings.SectionName).Get<AiSettings>() ?? new AiSettings();
        var budget = TimeSpan.FromSeconds(Math.Max(10, ai.TimeoutSeconds) + 60);

        services.AddRequestTimeouts(options => options.AddPolicy(AiGenerationTimeoutPolicy, budget));
        return services;
    }

    /// <summary>
    /// Behind a reverse proxy every client has the proxy's IP, so the per-IP rate limits would be shared by all
    /// users. List the proxy addresses in "ForwardedHeaders:TrustedProxies" to read X-Forwarded-For from them only.
    /// Nothing is trusted by default (an open list would let anyone fake their IP and dodge the limits).
    /// </summary>
    public static IServiceCollection AddPowerFitForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        var proxies = configuration.GetSection("ForwardedHeaders:TrustedProxies").Get<string[]>() ?? [];
        if (proxies.Length == 0) return services;

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            foreach (var proxy in proxies)
            {
                if (IPAddress.TryParse(proxy, out var address))
                    options.KnownProxies.Add(address);
            }
        });

        return services;
    }
}
