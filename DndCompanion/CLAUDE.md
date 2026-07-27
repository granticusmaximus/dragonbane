# CLAUDE.md — D&D Companion

Persistent context for Claude Code. Drop this in the repo root.

## What this is
A local-first desktop companion for D&D game nights: browse 2024 rules & items,
manage campaigns/characters, run the at-the-table loop (available actions,
equipment, spells/cantrips, previous actions, visual dice roller), and — later —
AI-assisted audio note-taking from the laptop mic.

## Stack (do not swap without asking)
.NET 10 · ASP.NET Core + Blazor · SQLite / EF Core · Electron.NET shell ·
Clean Architecture. Local AI later via Ollama; local STT via Whisper.net; mic via NAudio.

## Architecture
```
src/DndCompanion.Domain          entities, enums, value objects — NO dependencies
src/DndCompanion.Application     use-case interfaces + dice expression parser (pure)
src/DndCompanion.Infrastructure  EF Core + SQLite, DiceRoller, SRD import stub, DI
src/DndCompanion.UI              Blazor Razor Class Library (host-agnostic)
src/DndCompanion.Host            ASP.NET Core + Electron.NET shell
tests/DndCompanion.Tests         xUnit
```
Dependency rule: Domain ← Application ← Infrastructure/UI ← Host. Domain references nothing.

## Current state (Phase 1 scaffold — builds and runs)
- Solution + all six projects wired up; `dotnet build` and `dotnet test` are green
  (net10.0, EF Core 10.0.10, ElectronNET.API 23.6.2 — all resolved against the
  installed .NET 10 SDK with no version conflicts).
- Blazor `App` root, `Routes`, `MainLayout` live in `DndCompanion.Host/Components`;
  `DiceRoller` is mounted on a page in the `DndCompanion.UI` RCL (`Pages/DiceRollerPage.razor`,
  routed at `/` and `/dice-roller`) and registered via `AddAdditionalAssemblies` on
  the Host's endpoint mapping. `AddInfrastructure` wires `AppDbContext` + `IDiceRoller`.
- Initial EF Core migration created and applied; local SQLite DB lives at
  `src/DndCompanion.Host/bin/Debug/net10.0/dndcompanion.db`.
- `electronize start` (from `src/DndCompanion.Host`) runs Kestrel + the Electron
  window as one command — confirmed working end-to-end. Fixed one real bug along
  the way: `Program.cs` created the Electron window before `app.Run()`, racing
  Kestrel's startup; now uses `app.StartAsync()` / `app.WaitForShutdownAsync()`
  so the window never loads before the server is listening.
- `SrdImporter` is still a stub — next up (Phase 1 payload).
  `DiceRoller` + `DiceExpression` are complete and tested (5 passing tests).
- `DiceRoller.razor` shows the C#-driven pattern; rich SVG visuals still to port.
- Known non-blocking issue: `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 (transitive via
  EF Core Sqlite) has a NU1903 high-severity advisory
  (GHSA-2m69-gcr7-jv3q). Worth revisiting before shipping.

## Roadmap — build in usable slices, audio LAST
1. Reference + shell: SRD 5.2 import → browse/search rules, spells, items  ← next
2. Campaigns & characters: CRUD, character sheet, homebrew entry path
3. Play view + dice: actions/equipment/spells + roller + ActionEntry log
4. Audio transcript: NAudio → Whisper.net → live transcript
5. Audio structuring: Ollama drafts notes; user confirms

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
