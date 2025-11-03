using Microsoft.Extensions.Logging;
using AppTeste.Services;              
using AppTeste.ViewModels;            
using AppTeste.Views;
using CommunityToolkit.Maui;

namespace AppTeste
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()  
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });


            builder.Services.AddSingleton<IAuthService, GoogleAuthService>();

            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<ProfileViewModel>();

            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<ProfilePage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif
            return builder.Build();
        }
    }
}