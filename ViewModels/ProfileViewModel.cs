using AppTeste.Models;
using AppTeste.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AppTeste.ViewModels
{
    public partial class ProfileViewModel : ObservableObject
    {
        private readonly IAuthService _authService;

        [ObservableProperty]
        private User _user;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _tokenStatus;

        private Color _tokenStatusColor;
        public Color TokenStatusColor
        {
            get => _tokenStatusColor;
            set
            {
                if (_tokenStatusColor != value)
                {
                    _tokenStatusColor = value;
                    OnPropertyChanged();
                }
            }
        }

        public ProfileViewModel(IAuthService authService)
        {
            _authService = authService;
            _ = LoadUserDataAsync();
            TokenStatusColor = Colors.Green;
        }

        private async Task LoadUserDataAsync()
        {
            IsLoading = true;
            User = await _authService.GetCurrentUserAsync();

            if (User != null)
            {
                bool isValid = User.IsTokenValid();
                TokenStatus = isValid ? "✓ Token válido" : "✗ Token expirado";
                TokenStatusColor = isValid ? Colors.Green : Colors.Red; 
            }
            else
            {
                TokenStatus = "Usuário não encontrado";
                TokenStatusColor = Colors.Orange;
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
                Console.WriteLine($"Erro no logout: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}