using Android.App;
using Android.Content.PM;
using Android.Content;

namespace AppTeste
{
    [Activity(Theme = "@style/Maui.SplashTheme",
              MainLauncher = true,
              LaunchMode = LaunchMode.SingleTop,
              Exported = true,
              ConfigurationChanges = ConfigChanges.ScreenSize |
                                   ConfigChanges.Orientation |
                                   ConfigChanges.UiMode |
                                   ConfigChanges.ScreenLayout |
                                   ConfigChanges.SmallestScreenSize |
                                   ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnResume()
        {
            base.OnResume();
            Microsoft.Maui.ApplicationModel.Platform.OnResume(this);
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            Microsoft.Maui.ApplicationModel.Platform.OnNewIntent(intent);
        }
    }
}