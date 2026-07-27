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

## Current state (Phase 1 — reference + shell — done)
- Solution + all six projects wired up; `dotnet build` and `dotnet test` are green
  (net10.0, EF Core 10.0.10, ElectronNET.API 23.6.2, 9 passing tests).
- Blazor `App`/`Routes` live in `DndCompanion.Host/Components` (thin — no page logic).
  `MainLayout` (sidebar nav: Spells/Items/Actions/Dice Roller) and all pages live in
  `DndCompanion.UI` (`Layout/MainLayout.razor`, `Pages/*.razor`).
- `SrdImporter` parses `/data/*.json` into Spells/Items/ActionDefs, tags every row with
  a `ContentSource` (Srd + CC-BY attribution), and runs idempotently on every Host
  startup alongside `db.Database.MigrateAsync()` — no separate seed/migrate step needed.
  Data was parsed programmatically from the real SRD 5.2 text (5e24srd.com, itself
  sourced from the official `SRD_CC_v5.2.pdf`), not hand-authored — see README for
  the sourcing chain. 321 spells, 157 items, 13 actions seeded.
- Browse/search UI live at `/spells` (default page), `/items`, `/actions`: search box,
  toggleable filter chips (level/school on Spells, category on Items), dense list,
  right-side detail panel on row click. Verified interactively via Playwright + real
  Chrome (search, filters, row selection, detail panel content all confirmed working).
- Design system: dark-first CSS custom-property tokens in `Host/wwwroot/app.css`
  (light theme via `prefers-color-scheme`), Inter self-hosted as two variable-weight
  woff2 files in `Host/wwwroot/fonts/` (400–700 weight range, latin + latin-ext).
- `IRepository<T>` (already-designed interface in Application) now has a real impl:
  `EfRepository<T>` in Infrastructure, registered as an open generic. Uses scoped
  `AddDbContext`, not `AddDbContextFactory` — the existing `IRepository<T>` contract
  (`Remove` + `SaveChangesAsync` as separate calls) assumes one shared context per
  scope, which a per-operation factory can't satisfy without redesigning the interface.
- `electronize start` (from `src/DndCompanion.Host`) runs Kestrel + the Electron
  window as one command — confirmed working end-to-end, including click-interactivity.
- Testing gotcha (not a product bug): plain `dotnet run` without
  `ASPNETCORE_ENVIRONMENT=Development` boots in Production, where ASP.NET Core
  won't serve RCL/framework static web assets (`_framework/blazor.web.js`) from the
  dev-time provider — page loads but nothing is interactive. `electronize start`
  isn't affected since it runs through `dotnet publish`, which physically composes
  those assets into wwwroot regardless of environment.
- `DiceRoller.razor` still shows the original minimal C#-driven pattern (styled to
  the new design tokens); rich SVG visuals still to port — not in scope this round.
- Known non-blocking issue: `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 (transitive via
  EF Core Sqlite) has a NU1903 high-severity advisory
  (GHSA-2m69-gcr7-jv3q). Worth revisiting before shipping.

## Roadmap — build in usable slices, audio LAST
1. ~~Reference + shell: SRD 5.2 import → browse/search rules, spells, items~~ ← done
2. Campaigns & characters: CRUD, character sheet, homebrew entry path ← next
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
