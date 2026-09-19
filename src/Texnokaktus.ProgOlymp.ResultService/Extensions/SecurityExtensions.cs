using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using Texnokaktus.ProgOlymp.ResultService.Settings;

namespace Texnokaktus.ProgOlymp.ResultService.Extensions;

internal static class SecurityExtensions
{
    public static AuthenticationBuilder AddConfiguredJwtBearer(this AuthenticationBuilder builder,
                                                               IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()
                       ?? throw new("No JwtSettings in the configuration");

        return builder.AddJwtBearer(options => options.TokenValidationParameters = new()
        {
            ValidIssuer = jwtSettings.ClaimsIssuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwtSettings.IssuerSigningKey))
        });
    }
}
