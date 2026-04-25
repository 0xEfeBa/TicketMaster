namespace Identity.Infrastructure.Security;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 60;

    /// <summary>Refresh token ömrü (gün).</summary>
    public int RefreshTokenDays { get; set; } = 14;
}