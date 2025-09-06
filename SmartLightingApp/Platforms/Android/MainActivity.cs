using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace SmartLightingApp;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true,
    WindowSoftInputMode = SoftInput.AdjustResize | SoftInput.StateHidden,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        
        // Klavye ayarını zorla uygula
        Window?.SetSoftInputMode(SoftInput.AdjustResize);
    }
}