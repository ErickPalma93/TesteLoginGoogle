using AppTeste.Models;
using System.Text.Json;

namespace AppTeste.Services;

public class GoogleAuthService : IAuthService
{
    private const string ClientId = "556659463865-4bi59i533sfc8un7r807qgol183hedsh.apps.googleusercontent.com";

    private const string AuthorizeUrl = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string RedirectUri = "com.googleusercontent.apps.556659463865-4bi59i533sfc8un7r807qgol183hedsh:/oauth2redirect"; 
    private const string Scope = "openid profile email";
    private const string UserInfoEndpoint = "https://www.googleapis.com/oauth2/v2/userinfo";

    private User _currentUser;

    public async Task<User> LoginWithGoogleAsync()
    {
        try
        {
            var authUrl = $"{AuthorizeUrl}?client_id={ClientId}" +
                                  $"&redirect_uri={RedirectUri}" +
                                  $"&response_type=code" + 
                                  $"&scope={Scope}";

            System.Diagnostics.Debug.WriteLine($">>> Auth URL: {authUrl}");

            var authResult = await WebAuthenticator.Default.AuthenticateAsync(
                new Uri(authUrl),
                new Uri(RedirectUri));

            System.Diagnostics.Debug.WriteLine(">>> Auth concluído!");

            var code = authResult?.AccessToken;

            if (string.IsNullOrEmpty(code))
            {
                System.Diagnostics.Debug.WriteLine(">>> Code está vazio");
                return null;
            }

            System.Diagnostics.Debug.WriteLine($">>> Code recebido: {code}");

            var idToken = authResult?.IdToken;

            if (!string.IsNullOrEmpty(idToken))
            {
                System.Diagnostics.Debug.WriteLine($">>> ID Token recebido");
                var user = await FetchUserInfoFromIdToken(idToken);

                if (user != null)
                {
                    _currentUser = user;
                    await SaveUserToSecureStorageAsync(user);
                    return user;
                }
            }

            if (authResult?.Properties?.ContainsKey("access_token") == true)
            {
                var accessToken = authResult.Properties["access_token"];
                var user = await FetchUserInfoAsync(accessToken);

                if (user != null)
                {
                    _currentUser = user;
                    await SaveUserToSecureStorageAsync(user);
                }

                return user;
            }

            return null;
        }
        catch (TaskCanceledException)
        {
            System.Diagnostics.Debug.WriteLine(">>> Login cancelado pelo usuário");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($">>> Erro no login: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($">>> Stack trace: {ex.StackTrace}");
            throw new InvalidOperationException("Falha ao realizar login com Google", ex);
        }
    }

    public async Task LogoutAsync()
    {
        _currentUser = null;
        SecureStorage.Default.Remove("current_user");
        await Task.CompletedTask;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        if (_currentUser != null && _currentUser.IsTokenValid())
        {
            return true;
        }

        var user = await GetCurrentUserAsync();
        return user != null && user.IsTokenValid();
    }

    public async Task<User> GetCurrentUserAsync()
    {
        if (_currentUser != null && _currentUser.IsTokenValid())
        {
            return _currentUser;
        }

        var userJson = await SecureStorage.Default.GetAsync("current_user");

        if (!string.IsNullOrEmpty(userJson))
        {
            _currentUser = JsonSerializer.Deserialize<User>(userJson);

            if (_currentUser?.IsTokenValid() == true)
            {
                return _currentUser;
            }
        }

        return null;
    }

    private async Task<User> FetchUserInfoAsync(string accessToken)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            var response = await client.GetAsync(UserInfoEndpoint);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($">>> User Info JSON: {json}");

            var userInfo = JsonSerializer.Deserialize<GoogleUserInfo>(json);

            return new User
            {
                Id = userInfo.Id,
                Name = userInfo.Name,
                Email = userInfo.Email,
                ProfilePictureUrl = userInfo.Picture,
                AccessToken = accessToken
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($">>> Erro ao buscar informações do usuário: {ex.Message}");
            return null;
        }
    }

    private async Task<User> FetchUserInfoFromIdToken(string idToken)
    {
        try
        {
            var parts = idToken.Split('.');
            if (parts.Length != 3)
            {
                return null;
            }

            var payload = parts[1];

            var paddedPayload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            var decodedBytes = Convert.FromBase64String(paddedPayload);
            var decodedJson = System.Text.Encoding.UTF8.GetString(decodedBytes);

            System.Diagnostics.Debug.WriteLine($">>> ID Token Payload: {decodedJson}");

            var userInfo = JsonSerializer.Deserialize<GoogleIdTokenPayload>(decodedJson);

            return new User
            {
                Id = userInfo.Sub,
                Name = userInfo.Name,
                Email = userInfo.Email,
                ProfilePictureUrl = userInfo.Picture,
                AccessToken = idToken 
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($">>> Erro ao decodificar ID token: {ex.Message}");
            return null;
        }
    }

    private async Task SaveUserToSecureStorageAsync(User user)
    {
        var userJson = JsonSerializer.Serialize(user);
        await SecureStorage.Default.SetAsync("current_user", userJson);
    }

    private class GoogleUserInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Picture { get; set; }
    }

    private class GoogleIdTokenPayload
    {
        public string Sub { get; set; }  
        public string Name { get; set; }
        public string Email { get; set; }
        public string Picture { get; set; }
    }
}