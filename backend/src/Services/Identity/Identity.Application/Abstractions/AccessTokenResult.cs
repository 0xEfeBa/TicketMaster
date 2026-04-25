namespace Identity.Application.Abstractions;

public sealed record AccessTokenResult(string Token, int ExpiresInSeconds, string JwtId);
