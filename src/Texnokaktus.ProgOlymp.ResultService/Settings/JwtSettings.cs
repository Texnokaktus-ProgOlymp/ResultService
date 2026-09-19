namespace Texnokaktus.ProgOlymp.ResultService.Settings;

public class JwtSettings
{
    public string ClaimsIssuer { get; init; }
    public string Audience { get; init; }
    public string IssuerSigningKey { get; init; }
}
