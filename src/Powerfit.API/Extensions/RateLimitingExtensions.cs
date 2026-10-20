using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Powerfit.API.Responses;

namespace Powerfit.API.Extensions;

/// <summary>Names of the rate-limit policies (use them in [EnableRateLimiting(...)]).</summary>
public static class RateLimitPolicies
{
    /// <summary>Login / register: 10 requests per minute per IP address.</summary>
    public const string Auth = "auth";

    /// <summary>AI plan generation: 5 requests per hour per signed-in user (every call costs money).</summary>
    public const string AiGeneration = "ai-generation";
}

public static class RateLimitingExtensions
{
    public static IServiceCollection AddPowerFitRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(RateLimitPolicies.Auth, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

            options.AddPolicy(RateLimitPolicies.AiGeneration, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    // "uid" = UserId from the JWT (this limiter runs after authentication).
                    partitionKey: httpContext.User.FindFirst("uid")?.Value
                                  ?? httpContext.Connection.RemoteIpAddress?.ToString()
                                  ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromHours(1),
                        QueueLimit = 0
                    }));

            options.OnRejected = async (context, ct) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers["Retry-After"] =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }

                await ErrorResponse
                    .From(context.HttpContext, StatusCodes.Status429TooManyRequests, "Too many requests",
                        "You have sent too many requests. Please wait a while and try again.")
                    .WriteToAsync(context.HttpContext, ct);
            };
        });

        return services;
    }
}
