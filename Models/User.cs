namespace AppTeste.Models;

public class User
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ProfilePictureUrl { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    public DateTime AuthenticatedAt { get; set; } = DateTime.UtcNow;

    public bool IsTokenValid()
    {
        return TokenExpiresAt.HasValue && TokenExpiresAt.Value > DateTime.UtcNow;
    }
}