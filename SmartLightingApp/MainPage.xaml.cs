namespace SmartLightingApp;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
#if ANDROID
        Microsoft.Maui.Handlers.WebViewHandler.Mapper.AppendToMapping("CustomWebView", (handler, view) =>
        {
            handler.PlatformView.Settings.SetSupportZoom(false);
            handler.PlatformView.Settings.LoadWithOverviewMode = true;
        });
#endif
    }
}