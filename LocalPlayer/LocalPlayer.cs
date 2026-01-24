using Core;
using Core.Dto;
using NAudio.Wave;
using LocalPlayer.Exceptions;

namespace LocalPlayer;

/// <summary>
/// Local music backend implementing IMusicBackend using NAudio.
/// </summary>
public class LocalPlayer : IMusicBackend
{
    private IWavePlayer? _outputDevice;
    private AudioFileReader? _audioFile;

    public async Task PlayAsync(Song song)
    {
        if (string.IsNullOrWhiteSpace(song.SourcePath))
            throw new InvalidParameterException(nameof(song.SourcePath));

        if (!File.Exists(song.SourcePath))
            throw new InvalidParameterException($"File not found: {song.SourcePath}");

        await StopAsync();

        try
        {
            _audioFile = new AudioFileReader(song.SourcePath);
        }
        catch
        {
            throw new UnsupportedAudioFormatException(song.SourcePath);
        }

        _outputDevice = new WaveOutEvent();
        _outputDevice.Init(_audioFile);

        await Task.Run(() => _outputDevice.Play());
    }

    public async Task PauseAsync()
    {
        if (_outputDevice == null || _audioFile == null)
            throw new PlaybackStateException("pause", "stopped");

        await Task.Run(() => _outputDevice.Pause());
    }

    public async Task ResumeAsync()
    {
        if (_outputDevice == null || _audioFile == null)
            throw new PlaybackStateException("resume", "stopped");

        await Task.Run(() => _outputDevice.Play());
    }

    public async Task StopAsync()
    {
        await Task.Run(() =>
        {
            _outputDevice?.Stop();
            _outputDevice?.Dispose();
            _outputDevice = null;

            _audioFile?.Dispose();
            _audioFile = null;
        });
    }
}