using CommunityToolkit.Mvvm.ComponentModel; 
using CommunityToolkit.Mvvm.Input;          
using AppTeste.Models;                      
using AppTeste.Services;                    

namespace AppTeste.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IAuthService _authService;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private User _currentUser;

        [ObservableProperty]
        private bool _isAuthenticated;

        public LoginViewModel(IAuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _ = CheckAuthenticationAsync();
        }

        [RelayCommand]
        public async Task LoginWithGoogleAsync()
        {
            try
            {
                IsLoading = true;
                var user = await _authService.LoginWithGoogleAsync();

                if (user != null)
                {
                    if (Application.Current.MainPage is not AppShell)
                        Application.Current.MainPage = new AppShell();

                    await Shell.Current.GoToAsync("//ProfilePage");
                }
                else
                {
                    await App.Current.MainPage.DisplayAlert("Erro", "Falha ao fazer login", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro no login: {ex}");
                await App.Current.MainPage.DisplayAlert("Erro", "Ocorreu um problema ao fazer login", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }



        [RelayCommand]
        private async Task LogoutAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                await _authService.LogoutAsync();

                CurrentUser = null;
                IsAuthenticated = false;

                await Application.Current.MainPage.DisplayAlert(
                    "Logout",
                    "Você saiu com sucesso!",
                    "OK");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erro ao fazer logout: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Logout error: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }



        private async Task CheckAuthenticationAsync()
        {
            try
            {
                IsAuthenticated = await _authService.IsAuthenticatedAsync();

                if (IsAuthenticated)
                {
                    CurrentUser = await _authService.GetCurrentUserAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Check auth error: {ex}");
                IsAuthenticated = false;
            }
        }
    }
}