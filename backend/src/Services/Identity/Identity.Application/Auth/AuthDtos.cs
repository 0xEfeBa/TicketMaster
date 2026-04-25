namespace Identity.Application.Auth;

public record RegisterRequest(string Email, string Password);

public record LoginRequest(string Email, string Password);

public record AuthResponse(
    string AccessToken,
    int ExpiresIn,
    string TokenType,
    string RefreshToken,
    int RefreshExpiresIn);

public record RefreshTokenRequest(string RefreshToken);

public record LogoutRequest(string? RefreshToken);

public record UserMeResponse(Guid id, string email, string Role);

public record AssignRoleRequest(string Role);
