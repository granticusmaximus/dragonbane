# CLAUDE.md — D&D Companion

Persistent context for Claude Code. Drop this in the repo root.

## What this is
A local-first desktop companion for D&D game nights: browse 2024 rules & items,
manage campaigns/characters, run the at-the-table loop (available actions,
equipment, spells/cantrips, previous actions, visual dice roller), and — later —
AI-assisted audio note-taking from the laptop mic.

## Stack (do not swap without asking)
.NET 10 · ASP.NET Core + Blazor · SQLite / EF Core · Electron.NET shell ·
Clean Architecture. Local AI later via Ollama; local STT via Whisper.net; mic via
**PortAudioSharp2** (swapped from the originally-planned NAudio — confirmed via the
maintainer's own words that NAudio has no macOS recording support in either the
stable 2.x line or the 3.0 preview's new Linux/ALSA support; approved by the owner
before implementing). PortAudioSharp2 bundles real native binaries for osx-arm64.

## Architecture
```
src/DndCompanion.Domain          entities, enums, value objects — NO dependencies
src/DndCompanion.Application     use-case interfaces, IRepository<T>, dice expression parser (pure)
src/DndCompanion.Infrastructure  EF Core + SQLite, EfRepository<T>, DiceRoller, SrdImporter, DI
src/DndCompanion.UI              Blazor Razor Class Library (host-agnostic) — pages, layout, components
src/DndCompanion.Host            ASP.NET Core + Electron.NET shell — App/Routes only, no page logic
tests/DndCompanion.Tests         xUnit
data/{spells,items,actions}.json SRD 5.2 seed content (CC-BY-4.0), parsed from the real SRD text
```
Dependency rule: Domain ← Application ← Infrastructure/UI ← Host. Domain references nothing.
UI depends only on Application (`IRepository<T>`) — never on Infrastructure or EF Core directly;
Host's DI container is what wires `EfRepository<T>` in. This was violated once mid-session
(pages briefly injected `IDbContextFactory<AppDbContext>` directly) and corrected — watch for
this regression if extending the UI layer.

## Current state (Phases 1–5 done)
- Solution + all six projects wired up; `dotnet build` and `dotnet test` are green
  (net10.0, EF Core 10.0.10, ElectronNET.API 23.6.2, 45 passing tests).
- Blazor `App`/`Routes` live in `DndCompanion.Host/Components` (thin — no page logic).
  `MainLayout` (sidebar nav: Campaigns/Characters/Spells/Items/Actions/Dice Roller) and
  all pages live in `DndCompanion.UI` (`Layout/MainLayout.razor`, `Pages/*.razor`).
- `SrdImporter` parses `/data/*.json` into Spells/Items/ActionDefs, tags every row with
  a `ContentSource` (Srd + CC-BY attribution), and runs idempotently on every Host
  startup alongside `db.Database.MigrateAsync()` — no separate seed/migrate step needed.
  Data was parsed programmatically from the real SRD 5.2 text (5e24srd.com, itself
  sourced from the official `SRD_CC_v5.2.pdf`), not hand-authored — see README for
  the sourcing chain. 321 spells, 157 items, 13 actions seeded. Some real SRD content
  is still missing from this import (e.g. the Shield armor item, Faerie Fire,
  Thunderclap) — seeded ad hoc as Homebrew-tagged placeholders with backfill TODOs
  when a character needs them (see `DevSeed`); not a fabrication risk, just an
  incomplete scrape.
- `DevSeed.BaloneySlimAsync` (`Infrastructure/Dev/`) seeds one hand-authored test
  character, Development-only, idempotent by name. Real user campaign data
  ("Beyond the Blue Door", session zero) was created through the actual UI, not seeded.
- Browse/search UI live at `/spells`, `/items`, `/actions`: search box, toggleable
  filter chips, dense list, right-side detail panel on row click.
- Campaigns & Characters (`/campaigns`, `/characters` + detail pages): full CRUD,
  character sheet (identity, abilities, equipment/spells/actions with homebrew
  quick-add), campaign roster management, session log (title + date + notes) on the
  campaign detail page.
- Play view (`/characters/{id}/play`): session picker, ability check/save roller
  (real ability mods + optional proficiency + adv/normal/dis, via `IDiceRoller`),
  weapon damage roller (dice parsed from `Item.PropertiesJson` — no structured
  mechanical fields exist yet, so this reads a JSON convention, not a real column),
  session-scoped `ActionEntry` log (auto-populated from logged rolls, plus free-text
  manual entries). Attack-bonus math (proficiency + correct ability + magic bonus)
  is NOT modeled — weapon buttons roll base damage dice only, by design, rather than
  fabricate incomplete combat math.
- Design system: dark-first CSS custom-property tokens in `Host/wwwroot/app.css`
  (light theme via `prefers-color-scheme`), Inter self-hosted as two variable-weight
  woff2 files in `Host/wwwroot/fonts/` (400–700 weight range, latin + latin-ext).
- `IRepository<T>` (already-designed interface in Application) has a real impl:
  `EfRepository<T>` in Infrastructure, registered as an open generic, plus bespoke
  `EfCampaignRepository`/`EfCharacterRepository` for query methods beyond plain CRUD.
  Uses scoped `AddDbContext`, not `AddDbContextFactory`.
- **Known Blazor Server gotcha**: `AppDbContext` is scoped to the whole SignalR
  circuit (one browser tab session), not per-request. Two consequences hit so far:
  (1) entities with a client-generated `Guid Id` (e.g. `CharacterItem`,
  `CharacterSpell`, `ActionEntry`) must be added via their own `IRepository<T>.AddAsync`,
  never via collection-navigation mutation (`character.Items.Add(...)`) — EF's
  change tracker can't reliably tell Added from Modified once a non-default key is
  already set client-side, and silently issues an UPDATE that affects 0 rows. Entities
  with composite keys and no surrogate Id (`CampaignCharacter`) don't hit this, but the
  explicit-repo pattern is used everywhere for consistency. (2) Ordered `Include` (e.g.
  `.Include(c => c.Sessions.OrderBy(...))`) can silently stop reflecting the query's
  order once the collection is already tracked from an earlier write in the same
  circuit — sort client-side after loading instead, never rely on ordered Include for
  anything that gets mutated and re-read within one session.
- Every `<textarea>` must use `@bind:event="oninput"` explicitly — Blazor's default
  `@bind` on a textarea fires on `onchange` (blur), so a button wired to
  `disabled="@string.IsNullOrWhiteSpace(_field)"` stays disabled until the user clicks
  away first, and typed text can be silently lost if a button is clicked before blur.
  Found and fixed across all 6 textareas in the app; watch for this on any new one.
- `electronize start` (from `src/DndCompanion.Host`) runs Kestrel + the Electron
  window as one command. `electron.manifest.json` sets `"environment": "Development"`
  (no separate release-build process exists yet — revisit when one does). A `dnd-dev`
  shell function in `~/.zshrc` runs this from anywhere.
- Testing gotcha (not a product bug): plain `dotnet run` without
  `ASPNETCORE_ENVIRONMENT=Development` boots in Production, where ASP.NET Core
  won't serve RCL/framework static web assets (`_framework/blazor.web.js`) from the
  dev-time provider — page loads but nothing is interactive.
- `DiceRoller.razor` (the original Phase-1 component) still shows the minimal
  C#-driven pattern with hardcoded buttons; rich SVG visuals still to port. The real
  data-driven roller now lives in the Play view instead — the old component wasn't
  replaced, just superseded for actual play.
- `SQLitePCLRaw.lib.e_sqlite3` NU1903 (GHSA-2m69-gcr7-jv3q) — **fixed**. Pinned
  `SQLitePCLRaw.bundle_e_sqlite3` to 2.1.12 (bundles SQLite 3.53.3) via a direct
  PackageReference in Infrastructure, overriding EF Core Sqlite's transitive 2.1.11
  (which bundled 3.49.1, before the fix in 3.50.2).
- **Audio transcript** (`/sessions/{id}/record`, linked from each session on the
  campaign detail page): `PortAudioRecorder` (`Infrastructure/Audio/`) captures mono
  16 kHz mic audio; `WhisperTranscriptionService` runs it through Whisper.net
  (`ggml-base.bin`, downloaded on first use via `WhisperGgmlDownloader` to
  `{BaseDirectory}/models/` — ~148 MB, never committed to the repo). Recording page
  buffers ~10s chunks, transcribes each independently (no cross-chunk context — simpler,
  costs some accuracy on sentences split across a chunk boundary), persists every
  segment to `TranscriptSegment` live, and on Stop writes the full session audio to a
  real `.wav` file (`{BaseDirectory}/recordings/{recordingId}.wav`) referenced by
  `Recording.AudioPath`. `ITranscriptionService` was changed from `Stream` to
  `ReadOnlyMemory<float>` — it had never been implemented before this, and Whisper.net
  takes raw float samples directly, so a Stream/WAV round-trip would've been pure
  overhead. **Verified against a real live microphone**, not just unit tests — caught
  and fixed a real library bug in the process (see PortAudioRecorder's doc comment:
  `Stream.Stop()` then `Dispose()`/`using` throws from inside `Close()`; calling
  `Stop()` then `Close()` directly does not — the class never uses `Dispose()`/`using`
  on the underlying native stream). That live test also captured ~60s of real ambient
  room audio (not D&D-related) to prove the pipeline end-to-end; deleted the captured
  Recording/TranscriptSegment rows and the .wav file afterward rather than leave
  incidentally-captured audio sitting in the user's real database — worth remembering
  if testing this feature again.
- Whisper picks up ambient noise as short spurious phrases when there's no real
  speech (a known Whisper behavior on silence, not a bug) — no VAD/silence-gating is
  implemented, so expect noise in the transcript during quiet stretches. A future
  improvement, not required for this phase.
- **Audio structuring** (Phase 5, on the same Recording page): `OllamaNoteStructurer`
  (`Infrastructure/Notes/`) implements `INoteStructurer` against a local, already-running
  `ollama serve` (default `http://localhost:11434`, model `llama3.2` — chosen by the owner
  over larger/slower local models for this use case, since it runs repeatedly during a live
  session). Uses Ollama's structured-output `format` (JSON Schema) to force a
  `{"notes":[{kind, subject, text, tStart}]}` shape; response parsing is isolated in the
  pure, unit-tested `NoteDraftJsonParser` (Application layer) so the HTTP plumbing in
  Infrastructure stays thin. `RecordingPage` accumulates transcript segments into a rolling
  window and structures every ~60s of audio (6 chunks) or on a manual "Structure notes
  now" click, plus a final flush on Stop. Every draft is persisted immediately as a
  `StructuredNote` row with `Confirmed = false` (never lost if the app closes mid-session);
  the UI shows each draft with an editable text box and Confirm/Discard buttons — nothing
  is ever auto-committed. If Ollama is unreachable or returns something unparseable,
  `StructureAsync` degrades to an empty list rather than throwing — structuring is
  assistive, never required for recording/transcription to keep working. **Verified against
  the real local Ollama server** (not mocked): confirmed the model correctly categorizes
  Action/Loot/Location notes with right timestamps AND correctly omits a filler/small-talk
  line rather than fabricating a note for it.
- **Fixed data-location bug**: DB/model/recordings previously keyed off
  `AppContext.BaseDirectory`, the build output folder — which differs between
  `dotnet run` (`bin/Debug/net10.0`) and `electronize start` (`obj/Host/bin`). Real
  campaign data created while testing one way was invisible when launched the other
  way (looked like missing data, wasn't). Fixed with `IAppPaths` (Application
  abstraction, `AppPaths` impl in Infrastructure) pointing at one stable per-user dir:
  `~/Library/Application Support/DndCompanion/` on macOS. `Program.cs` and
  `RecordingPage.razor` both consume it now instead of computing `BaseDirectory`
  paths themselves. The real "Beyond the Blue Door" / Session Zero / Baloney Slim
  data (previously stranded in the `bin/Debug` copy) was migrated into the new path;
  the two stale build-output DB copies were deleted.

## Roadmap — build in usable slices, audio LAST
1. ~~Reference + shell: SRD 5.2 import → browse/search rules, spells, items~~ ← done
2. ~~Campaigns & characters: CRUD, character sheet, homebrew entry path~~ ← done
3. ~~Play view + dice: actions/equipment/spells + roller + ActionEntry log~~ ← done
4. ~~Audio transcript: PortAudioSharp2 → Whisper.net → live transcript~~ ← done
5. ~~Audio structuring: Ollama drafts notes; user confirms~~ ← done

All five roadmap phases are built. No further phase is queued — next work should come
from the owner (new feature request, bug, or polish pass) rather than an existing plan.

## Non-negotiable design decisions
- **Rules are data, not code.** Every rules row carries a `ContentSource`
  (Srd / Phb / Homebrew). Ship ONLY SRD 5.2 content (CC-BY-4.0). SRD 5.2 **excludes
  Aasimar** and other IP — those are user-entered Homebrew rows, never bundled.
- **Audio is assistive.** Transcription is reliable; who-did-what and
  initiative position are not. Initiative is explicit app state the user taps;
  audio annotates it. AI notes are drafts until confirmed — never auto-commit.
- Keep the `DndCompanion.UI` RCL host-agnostic so it can move to Photino/MAUI if
  Electron.NET fights .NET 10.

## Working style (owner's standing preferences)
- Accuracy over agreement — push back on flawed reasoning; don't just comply.
- Separate facts / inferences / assumptions / unknowns; give confidence levels on
  significant conclusions.
- Never fabricate APIs, packages, versions, or docs — say "I don't know" and verify.
- For technical calls, surface risks, edge cases, tradeoffs, and alternatives.
- Prefer SOLID / DI / testability; add tests with new logic; run the build and
  tests before claiming something works.

## SRD attribution (required on shipped SRD content)
"This work includes material from the System Reference Document 5.2 ('SRD 5.2') by
Wizards of the Coast LLC, available under the Creative Commons Attribution 4.0
International License."
