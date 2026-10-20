using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Powerfit.API.Responses;
using Powerfit.Business.Security;
using Powerfit.Business.Settings;

namespace Powerfit.API.Extensions;

/// <summary>Names of the authorization policies (use them in [Authorize(Policy = ...)]).</summary>
public static class AuthPolicies
{
    public const string UserOnly = "UserOnly";
    public const string AdminOnly = "AdminOnly";
}

public static class AuthenticationExtensions
{
    /// <summary>
    /// JWT bearer authentication + the UserOnly / AdminOnly policies.
    /// Every endpoint requires a signed-in user unless it has [AllowAnonymous] (fallback policy).
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();

        if (string.IsNullOrWhiteSpace(jwt.Key) || Encoding.UTF8.GetByteCount(jwt.Key) < 32)
            throw new InvalidOperationException("Jwt:Key is missing or shorter than 32 bytes.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Keep the short claim names written by TokenService (sub, uid, role, email).
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = "sub",
                    RoleClaimType = "role"
                };

                // 401 / 403 with the same JSON shape as every other error.
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();

                        var detail = context.AuthenticateFailure is SecurityTokenExpiredException
                            ? "The token has expired. Please sign in again."
                            : "Authentication is required. Send a valid Bearer token.";

                        context.Response.Headers["WWW-Authenticate"] = "Bearer";
                        await ErrorResponse
                            .From(context.HttpContext, StatusCodes.Status401Unauthorized, "Unauthorized", detail)
                            .WriteToAsync(context.HttpContext);
                    },
                    OnForbidden = context => ErrorResponse
                        .From(context.HttpContext, StatusCodes.Status403Forbidden, "Forbidden",
                            "You do not have permission to access this resource.")
                        .WriteToAsync(context.HttpContext)
                };
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(AuthPolicies.UserOnly, policy => policy.RequireAuthenticatedUser().RequireRole(AuthRoles.User))
            .AddPolicy(AuthPolicies.AdminOnly, policy => policy.RequireAuthenticatedUser().RequireRole(AuthRoles.Admin));

        return services;
    }
}
