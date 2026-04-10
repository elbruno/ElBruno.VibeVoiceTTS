using Plugin.Maui.Audio;
using VoiceLabs.Maui.Services;

namespace VoiceLabs.Maui;

public partial class MainPage : ContentPage
{
    private readonly VibeVoiceTtsService _ttsService;
    private readonly IAudioManager _audioManager;
    private IAudioPlayer? _audioPlayer;
    private byte[]? _currentAudio;
    private List<VoiceDisplayItem> _voices = [];

    public MainPage(VibeVoiceTtsService ttsService, IAudioManager audioManager)
    {
        InitializeComponent();
        _ttsService = ttsService;
        _audioManager = audioManager;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await InitializeModelAsync();
        LoadVoices();
    }

    private async Task InitializeModelAsync()
    {
        StatusIndicator.TextColor = Colors.Orange;
        StatusLabel.Text = "Downloading model...";

        try
        {
            var progress = new Progress<string>(message =>
            {
                MainThread.BeginInvokeOnMainThread(() => StatusLabel.Text = message);
            });

            await _ttsService.InitializeAsync(progress);

            StatusIndicator.TextColor = Colors.LimeGreen;
            StatusLabel.Text = "Model ready";
            GenerateButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            StatusIndicator.TextColor = Colors.Red;
            StatusLabel.Text = $"Model init failed: {ex.Message}";
            GenerateButton.IsEnabled = false;
        }
    }

    private void LoadVoices()
    {
        _voices = _ttsService.GetVoices();
        VoicePicker.ItemsSource = _voices;
        if (_voices.Count > 0)
            VoicePicker.SelectedIndex = 0;
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        CharCount.Text = $"{TextEditor.Text?.Length ?? 0} / 1000";
    }

    private void OnSampleClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string sample)
            TextEditor.Text = sample;
    }

    private async void OnGenerateClicked(object? sender, EventArgs e)
    {
        var text = TextEditor.Text?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            await DisplayAlert("Error", "Please enter some text.", "OK");
            return;
        }

        if (VoicePicker.SelectedItem is not VoiceDisplayItem voice)
        {
            await DisplayAlert("Error", "Please select a voice.", "OK");
            return;
        }

        GenerateButton.IsEnabled = false;
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        PlaybackFrame.IsVisible = false;

        try
        {
            _currentAudio = await _ttsService.GenerateAudioAsync(text, voice.Name);

            if (_currentAudio is { Length: > 0 })
            {
                PlaybackFrame.IsVisible = true;
            }
            else
            {
                await DisplayAlert("Error", "Failed to generate audio.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Generation failed: {ex.Message}", "OK");
        }
        finally
        {
            GenerateButton.IsEnabled = true;
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    private async void OnPlayClicked(object? sender, EventArgs e)
    {
        if (_currentAudio is null) return;

        try
        {
            _audioPlayer?.Dispose();
            var stream = new MemoryStream(_currentAudio);
            _audioPlayer = _audioManager.CreatePlayer(stream);
            _audioPlayer.Play();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Playback failed: {ex.Message}", "OK");
        }
    }

    private void OnStopClicked(object? sender, EventArgs e)
    {
        _audioPlayer?.Stop();
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (_currentAudio is null) return;

        try
        {
            var fileName = $"voicelabs_{DateTime.Now:yyyyMMdd_HHmmss}.wav";
            var filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);
            await File.WriteAllBytesAsync(filePath, _currentAudio);
            await DisplayAlert("Saved", $"Audio saved to:\n{filePath}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Save failed: {ex.Message}", "OK");
        }
    }
}
