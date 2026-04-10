using CommunityToolkit.Maui;
using ElBruno.VibeVoiceTTS.Extensions;
using Plugin.Maui.Audio;
using VoiceLabs.Maui.Services;

namespace VoiceLabs.Maui;

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

        // In-process ONNX TTS — no Python backend required
        builder.Services.AddVibeVoice(options =>
        {
            options.DiffusionSteps = 20;
        });

        builder.Services.AddSingleton<VibeVoiceTtsService>();
        builder.Services.AddSingleton(AudioManager.Current);
        builder.Services.AddTransient<MainPage>();

        return builder.Build();
    }
}
