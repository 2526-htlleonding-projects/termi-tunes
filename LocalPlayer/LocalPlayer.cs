using System.Diagnostics;
using System.Runtime.InteropServices;
using Core;
using Core.Dto;
using LibVLCSharp.Shared;
using LocalPlayer.Exceptions;

namespace LocalPlayer;

/// <summary>
/// Cross-platform local audio backend with LibVLC as the primary engine and
/// process-based fallback for environments without native LibVLC runtime.
/// </summary>
public sealed class LocalPlayer : IMusicBackend, IDisposable
{
    private static readonly string ExternalPlayerStateFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "termi-tunes",
        "external-player.pid");

    private readonly object _sync = new();
    private readonly LibVLC? _libVlc;
    private readonly MediaPlayer? _player;
    private Process? _externalPlayer;
    private bool _disposed;

    public LocalPlayer()
    {
        try
        {
            LibVLCSharp.Shared.Core.Initialize();
            _libVlc = new LibVLC("--quiet");
            _player = new MediaPlayer(_libVlc);
        }
        catch
        {
            _libVlc = null;
            _player = null;
        }
    }

    public Task PlayAsync(Song song)
    {
        EnsureNotDisposed();

        if (string.IsNullOrWhiteSpace(song.SourcePath))
        {
            throw new InvalidParameterException(nameof(song.SourcePath));
        }

        if (!File.Exists(song.SourcePath))
        {
            throw new InvalidParameterException($"File not found: {song.SourcePath}");
        }

        lock (_sync)
        {
            StopUnlocked();

            if (_player != null && _libVlc != null)
            {
                using var media = new Media(_libVlc, new Uri(Path.GetFullPath(song.SourcePath)));
                if (!_player.Play(media))
                {
                    throw new UnsupportedAudioFormatException($"Could not play local file: {song.SourcePath}");
                }

                return Task.CompletedTask;
            }

            _externalPlayer = StartExternalPlayer(song.SourcePath);
            PersistExternalPlayerPid(_externalPlayer.Id);
            return Task.CompletedTask;
        }
    }

    public Task PauseAsync()
    {
        EnsureNotDisposed();

        lock (_sync)
        {
            if (_player != null)
            {
                if (_player.State is VLCState.Stopped or VLCState.NothingSpecial or VLCState.Error)
                {
                    throw new PlaybackStateException("pause", "stopped");
                }

                _player.SetPause(true);
                return Task.CompletedTask;
            }

            AttachExternalPlayerFromStateIfNeeded();
            if (_externalPlayer == null || _externalPlayer.HasExited)
            {
                throw new PlaybackStateException("pause", "stopped");
            }

            SendSignal(_externalPlayer.Id, "STOP");
            return Task.CompletedTask;
        }
    }

    public Task ResumeAsync()
    {
        EnsureNotDisposed();

        lock (_sync)
        {
            if (_player != null)
            {
                if (_player.State is VLCState.Stopped or VLCState.NothingSpecial or VLCState.Error)
                {
                    throw new PlaybackStateException("resume", "stopped");
                }

                _player.SetPause(false);
                return Task.CompletedTask;
            }

            AttachExternalPlayerFromStateIfNeeded();
            if (_externalPlayer == null || _externalPlayer.HasExited)
            {
                throw new PlaybackStateException("resume", "stopped");
            }

            SendSignal(_externalPlayer.Id, "CONT");
            return Task.CompletedTask;
        }
    }

    public Task StopAsync()
    {
        EnsureNotDisposed();

        lock (_sync)
        {
            StopUnlocked();
            return Task.CompletedTask;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            StopUnlocked();
            _player?.Dispose();
            _libVlc?.Dispose();
            _disposed = true;
        }
    }

    private void StopUnlocked()
    {
        _player?.Stop();
        AttachExternalPlayerFromStateIfNeeded();

        if (_externalPlayer != null)
        {
            if (!_externalPlayer.HasExited)
            {
                _externalPlayer.Kill(true);
                _externalPlayer.WaitForExit(2000);
            }

            _externalPlayer.Dispose();
            _externalPlayer = null;
        }

        ClearExternalPlayerState();
    }

    private static Process StartExternalPlayer(string sourcePath)
    {
        if (CommandExists("ffplay"))
        {
            return StartProcess("ffplay", $"-nodisp -autoexit -loglevel error \"{sourcePath}\"");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
            Path.GetExtension(sourcePath).Equals(".wav", StringComparison.OrdinalIgnoreCase) &&
            CommandExists("aplay"))
        {
            return StartProcess("aplay", $"\"{sourcePath}\"");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) &&
            Path.GetExtension(sourcePath).Equals(".wav", StringComparison.OrdinalIgnoreCase) &&
            CommandExists("afplay"))
        {
            return StartProcess("afplay", $"\"{sourcePath}\"");
        }

        throw new LocalBackendUnavailableException(
            "No usable local playback backend is available. Install ffplay for full format support.");
    }

    private static Process StartProcess(string fileName, string arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardInput = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = true
            }
        };

        if (!process.Start())
        {
            throw new LocalBackendUnavailableException($"Could not start local player process: {fileName}");
        }

        return process;
    }

    private static bool CommandExists(string command)
    {
        var checker = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which";

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = checker,
                Arguments = command,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        process.WaitForExit();
        return process.ExitCode == 0;
    }

    private static void SendSignal(int pid, string signal)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new LocalBackendUnavailableException("Pause/resume signaling is not available for process fallback on Windows.");
        }

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "kill",
                Arguments = $"-s {signal} {pid}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd();
            throw new LocalBackendUnavailableException($"Failed to signal player process ({signal}): {error}");
        }
    }

    private void AttachExternalPlayerFromStateIfNeeded()
    {
        if (_externalPlayer is { HasExited: false })
        {
            return;
        }

        if (!File.Exists(ExternalPlayerStateFile))
        {
            return;
        }

        int pid;
        try
        {
            var text = File.ReadAllText(ExternalPlayerStateFile).Trim();
            if (!int.TryParse(text, out pid) || pid <= 0)
            {
                ClearExternalPlayerState();
                return;
            }
        }
        catch (IOException)
        {
            ClearExternalPlayerState();
            return;
        }
        catch (UnauthorizedAccessException)
        {
            ClearExternalPlayerState();
            return;
        }

        try
        {
            var process = Process.GetProcessById(pid);
            if (process.HasExited)
            {
                process.Dispose();
                ClearExternalPlayerState();
                return;
            }

            _externalPlayer = process;
        }
        catch (ArgumentException)
        {
            ClearExternalPlayerState();
        }
        catch (InvalidOperationException)
        {
            ClearExternalPlayerState();
        }
    }

    private static void PersistExternalPlayerPid(int pid)
    {
        var directory = Path.GetDirectoryName(ExternalPlayerStateFile);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new LocalBackendUnavailableException("Could not persist local player state.");
        }

        Directory.CreateDirectory(directory);
        File.WriteAllText(ExternalPlayerStateFile, pid.ToString());
    }

    private static void ClearExternalPlayerState()
    {
        if (!File.Exists(ExternalPlayerStateFile))
        {
            return;
        }

        File.Delete(ExternalPlayerStateFile);
    }

    private void EnsureNotDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(LocalPlayer));
        }
    }
}
