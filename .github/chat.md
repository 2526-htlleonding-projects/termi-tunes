# TermiTunes Codebase Review (Current State)

## Executive summary

The solution builds, but core runtime paths are currently broken in multiple places. The most severe issues are:

1. CLI bootstrapping constructs `PlaybackController` with uninitialized backends (`null`), so real playback paths will fail at runtime.
2. Data layer initialization and schema logic are inverted/inconsistent and can throw immediately or fail SQL operations.
3. Several command handlers drop async calls or ignore user input, so command behavior is incorrect even when parsing succeeds.
4. A large part of the advertised command surface is not actually parsed/executable from `CLI/Program.cs`.

---

## Critical logic flaws

### 1) CLI dependency initialization is missing (runtime null backends)

- `CLI/Program.cs` declares:
  - `private static readonly IMusicBackend _spotify;`
  - `private static readonly IMusicBackend _local;`
- Neither field is assigned before:
  - `var controller = new PlaybackController(_local, _spotify);`

**Impact:** controller is created with `null` dependencies. Calls like `Play/Pause/Resume` eventually invoke methods on `_local`/`_spotify` and can fail with `NullReferenceException`.

**Improve:**
- Instantiate concrete backends in `Main` (or via a composition root/DI).
- Fail fast with explicit configuration errors if a backend is unavailable.

---

### 2) CLI parser only registers a subset of verbs

- `CLI/Program.cs` parses only:
  - `PlayOptions, PauseOptions, ResumeOptions, LyricsOptions, SearchOptions`
- But `CLI/Verbs.cs` + `CommandMapper` define additional verbs (`queue`, `skip`, `back`, `cnick`, `add`, `c`, `l`, `ltheme`, `ctheme`).

**Impact:** many commands are effectively unreachable from CLI input even though mapping/command classes exist.

**Improve:**
- Register all verb option classes in `ParseArguments<...>()`.
- Keep `Verbs.cs`, parser registration, and `CommandMapper` synchronized.

---

### 3) Data service constructor always throws when initialization “succeeds”

- `Data/SongService.cs`:
  - `if (InitializeAsync().GetAwaiter().GetResult()) throw new DatabaseNotInitializedException();`
- `InitializeAsync()` returns `true` unconditionally.

**Impact:** `SongService` construction always throws `DatabaseNotInitializedException`.

**Improve:**
- Invert condition or return semantics (`false` on failure).
- Prefer an async factory/init pattern rather than blocking in constructor.

---

### 4) Database schema and queries are inconsistent

- Table creation:
  - `CREATE TABLE ... Nicknames (Id String Primary Key, Nickname String, Source Integer)`
- Queries/inserts use `SongId`:
  - `SELECT SongId, Source FROM Nicknames ...`
  - `INSERT OR REPLACE INTO Nicknames (Nickname, SongId, Source) ...`

**Impact:** SQL will fail because `SongId` column is missing from the created schema.

**Improve:**
- Align schema and query model (`SongId` vs `Id`) consistently.
- Add unique/index constraints for nickname lookup if nickname is key behavior.

---

## High-impact behavior bugs

### 5) Nickname existence logic is reversed

- `Data/SongService.cs`:
  - `bool alreadyExists = GetSongReferenceAsync(nickname).Result is null;`
  - `if (alreadyExists) throw new NicknameAlreadyExistsException(nickname);`

**Impact:** throws “already exists” when nickname does **not** exist.

**Improve:**
- Correct condition: existing reference should trigger the conflict.
- Avoid `.Result` on async method inside async method; use `await`.

---

### 6) Multiple commands fire async work without awaiting

- `Core/Commands/Pause.cs`, `Resume.cs`, `Stop.cs` call controller async methods but return `Task.CompletedTask`.
- `Core/Commands/Lyrics.cs`, `Search.cs`, `ChangePlaylist.cs` similarly drop tasks.

**Impact:** command reports completion before operation finishes; exceptions can be unobserved; ordering bugs are likely.

**Improve:**
- Make `ExecuteAsync` methods truly async and `await` controller calls.

---

### 7) User input is ignored in command implementations

- `Core/Commands/Search.cs` ignores `Query` and calls `controller.Search("smth")`.
- `Core/Commands/ChangeNick.cs` has `Target` but only passes `NewNick`.

**Impact:** CLI arguments do not drive expected behavior.

**Improve:**
- Pass the parsed user values through end-to-end.
- Add tests for option-to-command parameter propagation.

---

### 8) Local metadata provider contains guaranteed null-reference path

- `LocalPlayer/LocalMetadataProvider.cs`:
  - `List<Artist> artists = null;`
  - `artists!.Add(...)`

**Impact:** runtime `NullReferenceException` on metadata retrieval.

**Improve:**
- Initialize list (`new List<Artist>()`) before adding.
- Remove null-forgiving operator where value is actually null.

---

### 9) `Song` constructor dereferences nullable `sourcePath`

- `Core/Dto/Song.cs` constructor argument is `string? sourcePath`, but assigns:
  - `SourcePath = sourcePath.Trim();`

**Impact:** local/spotify object creation paths can throw if `sourcePath` is null (already reflected in nullable warning).

**Improve:**
- Use source-dependent null handling (e.g., allow null for Spotify, require non-null for Local).

---

### 10) Namespace layering issue in lyrics interface

- `Core/ILyricsProvider.cs` file is in `Core`, but namespace is `CLI`.
- `SpotifyPlayer` imports `using CLI;` to implement `ILyricsProvider`.

**Impact:** cross-layer coupling goes in the wrong direction (backend depending on CLI namespace).

**Improve:**
- Move `ILyricsProvider` to `Core` namespace and update references.

---

## Architecture and completeness gaps

### 11) Large surface still scaffolded with `NotImplementedException`

Examples:
- `PlaybackController.Search/Lyrics/Add/ChangeNick/ChangePlaylist`
- `Core/Commands/QueueCommand`, `Skip`, `ListPlaylists`, `ListThemes`, `ChangeTheme`
- Entire `SpotifyPlayer` backend

**Impact:** many user-visible commands are incomplete and will fail when called.

**Improve:**
- Define minimum vertical slice (e.g., local play + queue + nickname persistence) and complete it end-to-end before expanding surface.

---

### 12) TUI project is placeholder only

- `TUI/Program.cs` is still `"Hello, World!"`.

**Impact:** UI project is present in solution but currently non-functional relative to product goals.

**Improve:**
- Either scope it out of active solution work temporarily or implement an initial integrated TUI flow with `Core`.

---

## Quality and maintainability improvements

### 13) Warning debt is high and includes real correctness issues

Build output reports many nullable and async warnings (including concrete bugs listed above), plus unassigned fields in CLI startup.

**Improve:**
- Treat warnings in Core/CLI/Data/LocalPlayer as actionable bugs.
- Raise warning severity gradually or enforce no-new-warning policy.

---

### 14) Domain model/content hygiene

- `SongSource` includes an invalid/unprofessional enum value (`Nigga`) not used by logic.

**Impact:** harms maintainability and project professionalism; risks accidental behavior branches later.

**Improve:**
- Replace with intentional, documented source values only.

---

## Suggested remediation order

1. Fix startup wiring (`CLI/Program`) and parser verb registration.
2. Repair `SongService` init logic + schema/query consistency + nickname existence check.
3. Fix async command awaiting and parameter propagation (`Search`, `ChangeNick`, etc.).
4. Resolve nullability runtime bugs (`LocalMetadataProvider`, `Song.SourcePath` handling).
5. Normalize architecture boundaries (`ILyricsProvider` namespace/layering).
6. Complete one end-to-end vertical slice before implementing remaining placeholders.

