using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace ElBruno.VibeVoiceTTS.Extensions;

/// <summary>
/// Extension methods for registering VibeVoice services with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers VibeVoice TTS services with the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional action to configure <see cref="VibeVoiceOptions"/>.</param>
    /// <param name="configureTextToSpeech">Optional action to configure <see cref="VibeVoiceTextToSpeechOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddVibeVoice(
        this IServiceCollection services,
        Action<VibeVoiceOptions>? configure = null,
        Action<VibeVoiceTextToSpeechOptions>? configureTextToSpeech = null)
    {
        var options = new VibeVoiceOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddOptions<VibeVoiceTextToSpeechOptions>();
        if (configureTextToSpeech is not null)
        {
            services.Configure(configureTextToSpeech);
        }

        services.AddSingleton<VibeVoiceSynthesizer>(sp =>
        {
            var opts = sp.GetRequiredService<VibeVoiceOptions>();
            return new VibeVoiceSynthesizer(opts);
        });
        services.AddSingleton<IVibeVoiceSynthesizer>(sp => sp.GetRequiredService<VibeVoiceSynthesizer>());
        services.AddSingleton<VibeVoiceTextToSpeechClient>();
        services.AddSingleton<ITextToSpeechClient>(sp => sp.GetRequiredService<VibeVoiceTextToSpeechClient>());

        return services;
    }
}
