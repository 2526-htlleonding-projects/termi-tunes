# TermiTunes (Development README)

This repository contains a .NET solution for a terminal music player with local and Spotify-backed playback flows.

## Prerequisites

- .NET SDK 9.0+
- Linux/macOS/Windows terminal

## Project structure

- `CLI/` - command-line entrypoint and verb parsing
- `Core/` - orchestration (`PlaybackController`, command implementations, DTOs)
- `Data/` - persistence (`music.db` via `SongService`)
- `LocalPlayer/` - local playback and metadata integration
- `SpotifyPlayer/` - Spotify backend (currently mocked)
- `TUI/` - terminal UI placeholder

## Build and test

Run from the repository root:

```bash
dotnet build TermiTunes.sln
dotnet test TermiTunes.sln
```

## Run the CLI during development

Use `dotnet run` with `--` so arguments are passed to the app:

```bash
dotnet run --project CLI -- --help
dotnet run --project CLI -- play "song name"
dotnet run --project CLI -- search -f "victory lap"
```

## Local player usage

TermiTunes uses the `LocalPlayer` backend for local files:

- **Windows/macOS:** uses bundled native media runtime through LibVLC.
- **Linux:** tries LibVLC first, then falls back to available system players:
  - `ffplay` (best, supports many formats)
  - `aplay` for `.wav`

### Add and play local songs

Use absolute paths for best reliability:

```bash
dotnet run --project CLI -- play "/absolute/path/to/song.wav"
dotnet run --project CLI -- add "/absolute/path/to/song.wav" MyPlaylist
dotnet run --project CLI -- play MyPlaylist
```

### Playback controls

```bash
dotnet run --project CLI -- pause
dotnet run --project CLI -- resume
dotnet run --project CLI -- stop
```

### Switch playback mode

```bash
dotnet run --project CLI -- spotify-auth
dotnet run --project CLI -- spotify
dotnet run --project CLI -- local
```

### Linux note

For non-`.wav` local playback on Linux, install `ffplay` (ffmpeg package).

### Spotify authentication (phase 1)

Run `spotify-auth` to initialize Spotify OAuth. The CLI prompts for your Spotify Client ID, opens the browser for consent, waits for a local callback, and falls back to manual redirect-URL paste if needed. Tokens are stored in a separate local file at `~/.config/termi-tunes/spotify-auth.json`.

### Important

`run` is **not** a TermiTunes verb.  
Use `dotnet run --project CLI -- <verb> [args]` where `<verb>` is one of the CLI commands below.

## Current CLI verbs

- `play [target] [--shuffle|--smart_shuffle|--no_shuffle] [--loop]`
- `pause`
- `resume`
- `stop`
- `lyr [-a|--all]`
- `search <query> [-f|--first]`
- `queue <track>`
- `skip`
- `back`
- `cnick <target> <newNick>`
- `add [target] [playlist]`
- `c <playlistNameOrIndex>`
- `l`
- `lsongs [playlistNameOrIndex]`
- `ltheme`
- `ctheme <themeName>`
- `spotify-auth`
- `spotify`
- `local`
- `cdevice <deviceName>`
- `ldevice`

### Basic short aliases

- `p` -> `play`
- `h` -> `pause`
- `r` -> `resume`
- `s` -> `stop`

`lsongs` lists songs from the current playlist by default, or from a target playlist by name/index (example: `lsongs Recents`, `lsongs 0` or `lsongs '#0'`).

## Song autocomplete (bash/zsh)

Saved songs can be autocompleted by nickname, with songs in the current playlist ranked first.

Add this to your shell config (`~/.bashrc` or `~/.zshrc`):

```bash
if [ -n "${ZSH_VERSION:-}" ]; then autoload -U +X bashcompinit && bashcompinit; fi

alias tt='dotnet run --project /path/to/termi-tunes/CLI --'

_tt_complete() {
  local cur prev
  COMPREPLY=()
  cur="${COMP_WORDS[COMP_CWORD]}"
  prev="${COMP_WORDS[1]}"

  if [[ "$prev" == "play" || "$prev" == "p" || "$prev" == "queue" || "$prev" == "add" ]]; then
    mapfile -t COMPREPLY < <(tt complete-song "$cur" 2>/dev/null)
  fi
}

complete -F _tt_complete tt
```

For faster completion response, build once and use `--no-build` in the alias:

```bash
dotnet build /path/to/termi-tunes/TermiTunes.sln
alias tt='dotnet run --no-build --project /path/to/termi-tunes/CLI --'
```

### Fish

Use a function plus a dedicated completion file (more reliable than alias-based setup):

`~/.config/fish/config.fish`

```fish
function tt
    dotnet run --no-build --project /path/to/termi-tunes/CLI -- $argv
end
```

`~/.config/fish/completions/tt.fish`

```fish
complete -e -c tt

function __tt_song_complete
    set -l token (commandline -ct)
    tt complete-song "$token" --limit 50 2>/dev/null
end

complete -c tt -f -n '__fish_seen_subcommand_from play p queue add' -a '(__tt_song_complete)'
```

Then reload fish:

```fish
source ~/.config/fish/config.fish
source ~/.config/fish/completions/tt.fish
```

Quick checks:

```fish
type -a tt
complete -p tt
```

## Common development workflow

1. Make code changes in the relevant project(s).
2. Build the full solution.
3. Run the CLI command you changed via `dotnet run --project CLI -- ...`.
4. Run the solution tests.

## Adding a new CLI command

Keep these in sync:

1. Add the verb options class in `CLI/Verbs.cs`.
2. Map options to a command in `CLI/CommandMapper.cs`.
3. Add/implement the command in `Core/Commands/*`.
4. Register the options type in `CLI/Program.cs` parser setup.
