# Spotify Implementation Kickoff (2026-04-22)

## Current state (from codebase)
- `SpotifyPlayer/SpotifyPlayer.cs` is still a placeholder backend (console prints only).
- CLI already has mode verbs (`spotify`, `local`) and routes Spotify songs through the Spotify backend.
- Song model already supports Spotify tracks (`SongSource.Spotify`) and Spotify IDs.
- Lyrics currently only run for Spotify songs, but provider logic is still mocked.

## Draft implementation plan (pending your answers)
1. **Integration foundation**
   - Choose Spotify integration approach (Spotify Connect control vs browser playback SDK style flow for this CLI).
   - Add config model for credentials and runtime settings.
   - Implement auth/token flow and secure token persistence.
2. **Backend core implementation**
   - Replace placeholder `SpotifyPlayer` with real API-backed implementation of `PlayAsync`, `PauseAsync`, `ResumeAsync`, `StopAsync`.
   - Implement active-device handling and map it to existing `cdevice` / `ldevice` flows.
3. **Catalog + song resolution**
   - Define how `play <query>` should resolve Spotify results (top result, prompt, or strict exact match).
   - Normalize Spotify track metadata into `SongFactory.CreateSpotify(...)`.
4. **Lyrics strategy**
   - Decide whether lyrics come from Spotify APIs (if available for your app type) or a separate provider.
5. **UX + reliability**
   - Add clear user-facing errors for auth required, no active device, region restrictions, and unavailable tracks.
   - Keep local mode behavior unchanged.
6. **Tests + docs**
   - Add/extend unit tests for command flow and controller/backend routing.
   - Update README with setup, auth steps, device rules, and command examples.

## Questions (please answer here in this file)
1. Which Spotify capability do you want first?
   - A) Control an already-running Spotify app/device (Connect control)
   - B) In-app audio playback directly from this app
   - C) Both

First, I want to implement the Auth. Then the playback controll through a headless browser in the back. And at last the lyric support.

2. Should the first milestone include authentication now, or do you want a mocked token flow first?

The very fist thing will be Auth.

3. For `play <query>` in Spotify mode, which behavior do you want?
   - A) Auto-play first result
   - B) Require explicit selection
   - C) Exact match only, otherwise fail

exact mathes through autocomlete

4. Should Spotify search/add operate only on tracks, or also albums/playlists/artists in phase 1?

playlist and albums will be added later, artists aren'nt planned for now.

5. Do you want lyrics included in phase 1, or postponed?

later.

6. Should device selection (`cdevice` / `ldevice`) be fully wired to Spotify devices in phase 1?

yes.

7. How should credentials be provided?
   - A) Environment variables
   - B) Config file
   - C) Interactive setup command
   - D) Combination (specify)

an init command that prompts the user to log in, prefferably in the terminal but there should be a way to do it in the browser aswell.    

9. Any explicit out-of-scope items for phase 1?

Lyrics, Playback, anyting thats not auth.

## Follow-up questions (blocking implementation details)
1. **Phase-1 scope confirmation:** should phase 1 be **auth only** (no `play/pause/resume/stop`, no device control wiring), or do you also want device control now?
2. **Init command name:** do you want the new verb to be `spotify-init`, `spotify-auth`, or just `init`?
3. **Terminal vs browser login flow:** Spotify auth requires browser-based consent. Is this acceptable flow?
   - Terminal command starts login
   - We auto-open your browser to Spotify consent page
   - After approval, callback is captured locally (with manual paste fallback if needed)
4. **Credential inputs at init time:** should `client_id` be entered interactively during `init`, read from environment variables, or support both?
5. **Token storage location:** okay to store access/refresh tokens in app settings (backed by `music.db`) for now?
