using AppTeste.Models;
using AppTeste.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppTeste.ViewModels
{
    public partial class ProfileViewModel : ObservableObject
    {
        private readonly IAuthService _authService;

        [ObservableProperty]
        private User? _user;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _tokenStatus = string.Empty;

        [ObservableProperty]
        private Color _tokenStatusColor = Colors.Green;

        public ProfileViewModel(IAuthService authService)
        {
            _authService = authService;
            _ = LoadUserDataAsync();
        }

        private async Task LoadUserDataAsync()
        {
            IsLoading = true;

            System.Diagnostics.Debug.WriteLine(">>> ProfileViewModel: Carregando dados do usuário...");

            User = await _authService.GetCurrentUserAsync();

            if (User != null)
            {
                System.Diagnostics.Debug.WriteLine($">>> ProfileViewModel: Usuário carregado - {User.Name}");

                bool isValid = User.IsTokenValid();
                TokenStatus = isValid ? "✓ Token válido" : "✗ Token expirado";
                _tokenStatusColor = isValid ? Colors.Green : Colors.Red;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(">>> ProfileViewModel: ❌ Usuário não encontrado!");
                TokenStatus = "Usuário não encontrado";
                _tokenStatusColor = Colors.Orange;
            }

            IsLoading = false;
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            try
            {
                await _authService.LogoutAsync();
                await Shell.Current.GoToAsync("//LoginPage");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($">>> Erro no logout: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}