using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Extensions.Logging;
using SmartLightingApp.Services;
using TasmotaSharp;

namespace SmartLightingApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<TasmotaMdnsDiscoveryService>();
        builder.Services.AddTransient<TasmotaClient>();
        builder.Services.AddSingleton<RelayDataService>();
 
        return builder.Build();
    }
}