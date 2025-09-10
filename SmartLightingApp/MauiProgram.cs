using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Extensions.Logging;
using SmartLightingApp.Services;
using SMG.Localization.Generated;
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

        // HttpClient factory'yi önce kaydet
        builder.Services.AddHttpClient();
        
        // TasmotaClient'ı factory pattern ile kaydet (daha güvenilir)
        builder.Services.AddTransient<TasmotaClient>(provider =>
        {
            var httpClientFactory = provider.GetService<IHttpClientFactory>();
            var logger = provider.GetService<ILogger<TasmotaClient>>();
            return new TasmotaClient(logger: logger, httpClientFactory: httpClientFactory);
        });
        
        builder.Services.AddSingleton<TasmotaMdnsDiscoveryService>();
        builder.Services.AddSingleton<RelayDataService>();
        
        builder.Services.AddLocalization(options =>
        {
            options.DefaultLanguage = "en";
            options.FallbackLanguage = "en";
        });
        
        return builder.Build();
    }
}