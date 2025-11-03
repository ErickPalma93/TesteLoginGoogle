using AppTeste.Models;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace AppTeste.Services;

public class GoogleAuthService : IAuthService
{
    private const string ClientId = "556659463865-4bi59i533sfc8un7r807qgol183hedsh.apps.googleusercontent.com";
    private const string AuthorizeUrl = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenUrl = "https://oauth2.googleapis.com/token";
    private const string RedirectUri = "com.googleusercontent.apps.556659463865-4bi59i533sfc8un7r807qgol183hedsh:/oauth2redirect";
    private const string Scope = "openid profile email";
    private const string UserInfoEndpoint = "https://www.googleapis.com/oauth2/v2/userinfo";

    private User? _currentUser;
    private string? _codeVerifier;

    public async Task<User?> LoginWithGoogleAsync()
    {
        try
        {
            _codeVerifier = GenerateCodeVerifier();
            var codeChallenge = GenerateCodeChallenge(_codeVerifier);

            var authUrl = $"{AuthorizeUrl}?client_id={ClientId}" +
                          $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
                          $"&response_type=code" +
                          $"&scope={Uri.EscapeDataString(Scope)}" +
                          $"&code_challenge={codeChallenge}" +
                          $"&code_challenge_method=S256" +
                          $"&access_type=offline";

            System.Diagnostics.Debug.WriteLine($">>> Auth URL: {authUrl}");

            var authResult = await WebAuthenticator.Default.AuthenticateAsync(
                new Uri(authUrl),
                new Uri(RedirectUri));

            System.Diagnostics.Debug.WriteLine(">>> Auth concluído!");

            var code = authResult?.Properties?.ContainsKey("code") == true
                ? authResult.Properties["code"]
                : null;

            if (string.IsNullOrEmpty(code))
            {
                System.Diagnostics.Debug.WriteLine(">>> ❌ Authorization code não recebido");
                if (authResult?.Properties != null)
                {
                    foreach (var prop in authResult.Properties)
                    {
                        System.Diagnostics.Debug.WriteLine($">>>   {prop.Key}: {prop.Value}");
                    }
                }
                return null;
            }

            System.Diagnostics.Debug.WriteLine($">>> ✓ Authorization Code recebido");

            var tokenResponse = await ExchangeCodeForTokenAsync(code);

            if (tokenResponse == null)
            {
                System.Diagnostics.Debug.WriteLine(">>> ❌ Falha ao trocar code por token");
                return null;
            }

            System.Diagnostics.Debug.WriteLine($">>> ✓ Tokens recebidos!");
            System.Diagnostics.Debug.WriteLine($">>>   Access Token: {!string.IsNullOrEmpty(tokenResponse.AccessToken)}");
            System.Diagnostics.Debug.WriteLine($">>>   ID Token: {!string.IsNullOrEmpty(tokenResponse.IdToken)}");
            System.Diagnostics.Debug.WriteLine($">>>   Refresh Token: {!string.IsNullOrEmpty(tokenResponse.RefreshToken)}");

            User? user = null;

            // Tentar ID Token primeiro
            if (!string.IsNullOrEmpty(tokenResponse.IdToken))
            {
                System.Diagnostics.Debug.WriteLine(">>> Tentando obter dados do ID Token...");
                user = await FetchUserInfoFromIdToken(tokenResponse.IdToken);

                if (user != null)
                {
                    System.Diagnostics.Debug.WriteLine($">>> ✓ Usuário obtido do ID Token: {user.Name}");
                    user.AccessToken = tokenResponse.AccessToken ?? string.Empty;
                    user.RefreshToken = tokenResponse.RefreshToken;
                    user.TokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
                    user.AuthenticatedAt = DateTime.UtcNow;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(">>> ❌ Falha ao obter dados do ID Token");
                }
            }

            // Fallback: usar Access Token
            if (user == null && !string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                System.Diagnostics.Debug.WriteLine(">>> Tentando obter dados via Access Token...");
                user = await FetchUserInfoAsync(tokenResponse.AccessToken);

                if (user != null)
                {
                    System.Diagnostics.Debug.WriteLine($">>> ✓ Usuário obtido via API: {user.Name}");
                    user.RefreshToken = tokenResponse.RefreshToken;
                    user.TokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
                    user.AuthenticatedAt = DateTime.UtcNow;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(">>> ❌ Falha ao obter dados via API");
                }
            }

            if (user != null)
            {
                System.Diagnostics.Debug.WriteLine(">>> ✓ Salvando usuário...");
                _currentUser = user;
                await SaveUserToSecureStorageAsync(user);

                System.Diagnostics.Debug.WriteLine($">>> ✓ Login completo!");
                System.Diagnostics.Debug.WriteLine($">>>   Nome: {user.Name}");
                System.Diagnostics.Debug.WriteLine($">>>   Email: {user.Email}");
                System.Diagnostics.Debug.WriteLine($">>>   ID: {user.Id}");

                return user;
            }

            System.Diagnostics.Debug.WriteLine(">>> ❌ Não foi possível obter dados do usuário");
            return null;
        }
        catch (TaskCanceledException)
        {
            System.Diagnostics.Debug.WriteLine(">>> Login cancelado pelo usuário");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($">>> ❌ ERRO no login: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($">>> Stack: {ex.StackTrace}");
            return null;
        }
    }

    private async Task<TokenResponse?> ExchangeCodeForTokenAsync(string code)
    {
        try
        {
            if (string.IsNullOrEmpty(_codeVerifier))
            {
                System.Diagnostics.Debug.WriteLine(">>> ❌ Code verifier não encontrado");
                return null;
            }

            using var client = new HttpClient();

            var parameters = new Dictionary<string, string>
            {
                { "client_id", ClientId },
                { "code", code },
                { "code_verifier", _codeVerifier },
                { "redirect_uri", RedirectUri },
                { "grant_type", "authorization_code" }
            };

            var content = new FormUrlEncodedContent(parameters);

            System.Diagnostics.Debug.WriteLine(">>> Trocando code por token...");

            var response = await client.PostAsync(TokenUrl, content);
            var json = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine($">>> Status: {response.StatusCode}");
            System.Diagnostics.Debug.WriteLine($">>> Response: {json}");

            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($">>> ❌ Erro HTTP ao trocar token");
                return null;
            }

            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return tokenResponse;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($">>> ❌ Exceção ao trocar token: {ex.Message}");
            return null;
        }
    }

    private string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private string GenerateCodeChallenge(string codeVerifier)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(codeVerifier);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public Task LogoutAsync()
    {
        _currentUser = null;
        SecureStorage.Default.Remove("current_user");
        return Task.CompletedTask;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var user = await GetCurrentUserAsync();
        return user != null && user.IsTokenValid();
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        if (_currentUser != null && _currentUser.IsTokenValid())
        {
            System.Diagnostics.Debug.WriteLine($">>> Usuário em memória: {_currentUser.Name}");
            return _currentUser;
        }

        var userJson = await SecureStorage.Default.GetAsync("current_user");

        if (!string.IsNullOrEmpty(userJson))
        {
            System.Diagnostics.Debug.WriteLine($">>> Carregando usuário do storage...");
            _currentUser = JsonSerializer.Deserialize<User>(userJson);

            if (_currentUser != null)
            {
                System.Diagnostics.Debug.WriteLine($">>> Usuário carregado: {_currentUser.Name}");
                System.Diagnostics.Debug.WriteLine($">>> Token válido: {_currentUser.IsTokenValid()}");

                if (_currentUser.IsTokenValid())
                {
                    return _currentUser;
                }
            }
        }

        System.Diagnostics.Debug.WriteLine(">>> Nenhum usuário autenticado encontrado");
        return null;
    }

    private async Task<User?> FetchUserInfoAsync(string accessToken)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            var response = await client.GetAsync(UserInfoEndpoint);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($">>> User Info JSON: {json}");

            var userInfo = JsonSerializer.Deserialize<GoogleUserInfo>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (userInfo == null)
            {
                System.Diagnostics.Debug.WriteLine(">>> ❌ Falha ao deserializar UserInfo");
                return null;
            }

            return new User
            {
                Id = userInfo.Id ?? string.Empty,
                Name = userInfo.Name ?? "Sem nome",
                Email = userInfo.Email ?? "Sem email",
                ProfilePictureUrl = userInfo.Picture ?? string.Empty,
                AccessToken = accessToken
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($">>> ❌ Erro ao buscar UserInfo: {ex.Message}");
            return null;
        }
    }

    private Task<User?> FetchUserInfoFromIdToken(string idToken)
    {
        try
        {
            var parts = idToken.Split('.');
            if (parts.Length != 3)
            {
                System.Diagnostics.Debug.WriteLine($">>> ❌ ID Token inválido (partes: {parts.Length})");
                return Task.FromResult<User?>(null);
            }

            var payload = parts[1];
            var paddedPayload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            var decodedBytes = Convert.FromBase64String(paddedPayload);
            var decodedJson = Encoding.UTF8.GetString(decodedBytes);

            System.Diagnostics.Debug.WriteLine($">>> ID Token Payload: {decodedJson}");

            var userInfo = JsonSerializer.Deserialize<GoogleIdTokenPayload>(decodedJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (userInfo == null)
            {
                System.Diagnostics.Debug.WriteLine(">>> ❌ Falha ao deserializar ID Token");
                return Task.FromResult<User?>(null);
            }

            System.Diagnostics.Debug.WriteLine($">>> Dados extraídos do token:");
            System.Diagnostics.Debug.WriteLine($">>>   Sub: {userInfo.Sub}");
            System.Diagnostics.Debug.WriteLine($">>>   Name: {userInfo.Name}");
            System.Diagnostics.Debug.WriteLine($">>>   Email: {userInfo.Email}");

            return Task.FromResult<User?>(new User
            {
                Id = userInfo.Sub ?? string.Empty,
                Name = userInfo.Name ?? "Sem nome",
                Email = userInfo.Email ?? "Sem email",
                ProfilePictureUrl = userInfo.Picture ?? string.Empty
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($">>> ❌ Erro ao decodificar ID token: {ex.Message}");
            return Task.FromResult<User?>(null);
        }
    }

    private async Task SaveUserToSecureStorageAsync(User user)
    {
        try
        {
            var userJson = JsonSerializer.Serialize(user);
            await SecureStorage.Default.SetAsync("current_user", userJson);
            System.Diagnostics.Debug.WriteLine(">>> ✓ Usuário salvo no SecureStorage");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($">>> ❌ Erro ao salvar usuário: {ex.Message}");
        }
    }

    private class TokenResponse
    {
        public string? Access_Token { get; set; }
        public string? AccessToken => Access_Token;

        public string? Id_Token { get; set; }
        public string? IdToken => Id_Token;

        public string? Refresh_Token { get; set; }
        public string? RefreshToken => Refresh_Token;

        public int Expires_In { get; set; }
        public int ExpiresIn => Expires_In;

        public string? Token_Type { get; set; }
        public string? Scope { get; set; }
    }

    private class GoogleUserInfo
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Picture { get; set; }
    }

    private class GoogleIdTokenPayload
    {
        public string? Sub { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Picture { get; set; }
    }
}