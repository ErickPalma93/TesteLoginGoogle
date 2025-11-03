using AppTeste.ViewModels;

namespace AppTeste.Views;

public partial class ProfilePage : ContentPage
{
    public ProfilePage(ProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        System.Diagnostics.Debug.WriteLine(">>> ProfilePage: Construtor chamado!");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        System.Diagnostics.Debug.WriteLine(">>> ProfilePage: OnAppearing chamado!");
    }
}