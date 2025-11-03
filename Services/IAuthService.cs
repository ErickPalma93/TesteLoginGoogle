using AppTeste.Models;

namespace AppTeste.Services
{
    public interface IAuthService
    {
        Task<User> LoginWithGoogleAsync();
        Task LogoutAsync();
        Task<bool> IsAuthenticatedAsync();
        Task<User> GetCurrentUserAsync();
    }
}