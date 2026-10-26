using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests;

internal static class TestJwtTokens
{
    private const string Issuer = "result-service-integration-tests";
    private const string Audience = "result-service";
    private static readonly SymmetricSecurityKey SigningKey = new("integration-tests-only-signing-key-1234567890"u8.ToArray());

    public static void Configure(JwtBearerOptions options)
    {
        options.Authority = null;
        options.MetadataAddress = string.Empty;
        options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(new());
        options.MapInboundClaims = true;
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidIssuer = Issuer,
            ValidateAudience = true,
            ValidAudience = Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = SigningKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    }

    public static string Create(int userId, bool expired = false, bool invalidSignature = false)
    {
        var now = DateTime.UtcNow;
        var key = invalidSignature
            ? new SymmetricSecurityKey("another-integration-test-signing-key-1234567890"u8.ToArray())
            : SigningKey;
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: [new(ClaimTypes.NameIdentifier, userId.ToString(CultureInfo.InvariantCulture))],
            notBefore: now.AddMinutes(-10),
            expires: expired ? now.AddMinutes(-5) : now.AddMinutes(5),
            signingCredentials: new(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
