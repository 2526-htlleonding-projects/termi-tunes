# Copilot Instructions for `termi-tunes`

## Build, test, and lint commands

Use the solution root (`/home/arthur/RiderProjects/termi-tunes`) for commands.

- Build all projects:
  - `dotnet build TermiTunes.sln`
- Run all tests:
  - `dotnet test TermiTunes.sln`
- Run a single test (when test projects exist):
  - `dotnet test TermiTunes.sln --filter "FullyQualifiedName~Namespace.ClassName.TestName"`
- Run a single CLI command:
  - `dotnet run --project CLI -- <verb> [args]`

There is currently no dedicated lint command configured in the repository (no lint config files or lint-oriented project setup).

## High-level architecture

The repository is a .NET solution composed of `CLI`, `Core`, `LocalPlayer`, `SpotifyPlayer`, `Data`, and `TUI`.

- `CLI` is the executable entrypoint. It parses verbs/options with `CommandLineParser`, maps parsed options via `CommandMapper`, and executes a `Core.Commands.ICommand` against `PlaybackController`.
- `Core` is the orchestration layer:
  - `PlaybackController` owns playback state and routing logic.
  - `IMusicBackend`, `IMetadataProvider`, `ILyricsProvider`, and `IPlaybackStore` define extension points for integrations and persistence.
  - `Core/Commands/*` is a command layer that wraps controller actions.
  - `Core/Dto/*` holds domain DTOs and `SongFactory`.
- `LocalPlayer` provides local playback (`NAudio`) and local metadata extraction (`TagLibSharp`) and implements `IMusicBackend`/`IMetadataProvider`.
- `SpotifyPlayer` is a currently mocked Spotify backend implementation for CLI flow completeness (`IMusicBackend`, `ILyricsProvider`).
- `Data/SongService` implements `IPlaybackStore` and persists songs, nicknames, playlists, queue/history, and settings/device state in `music.db`.
- `TUI` currently contains placeholder code and is not integrated with the CLI/Core flow.

## Key conventions in this codebase

- **Command wiring pattern is split across three layers**:
  1. CLI option class with `[Verb(...)]` in `CLI/Verbs.cs`
  2. Option-to-command mapping in `CLI/CommandMapper.cs`
  3. Command implementation in `Core/Commands/*` implementing `ICommand`
  
  Keep these three in sync for any new CLI command.

- **Playback backend selection is source-driven**: `PlaybackController` routes operations to `_local` or `_spotify` based on `Song.Source` rather than command-specific branching.

- **Song creation is expected through `SongFactory`**: IDs and default nicknames are generated there, including normalization for nickname defaults (lowercase with non-alphanumerics collapsed to `_`).

- **CLI flag compatibility includes shorthand normalization in `CLI/Program.cs`**: `-ss` is normalized to `--smart_shuffle` and `-ns` to `--no_shuffle` before parsing.

- **Exception types are scoped per project**: `Core`, `CLI`, `LocalPlayer`, and `Data` each define their own base exception hierarchy and derive module-specific exceptions from those bases.
