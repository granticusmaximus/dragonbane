# D&D Companion

A local-first desktop companion for D&D game nights: browse 2024 rules & items,
manage campaigns and characters, run the at-the-table loop (actions, equipment,
spells, dice, action log), and — later — AI-assisted audio note-taking.

Stack: **.NET 10 · ASP.NET Core + Blazor · SQLite/EF Core · Electron.NET**, in
**Clean Architecture**. Phase 1 (reference + shell) is scaffolded, builds, and
runs — see [CLAUDE.md](CLAUDE.md) for the detailed current state and roadmap.

## Layout
```
src/
  DndCompanion.Domain          entities, enums, value objects (no dependencies)
  DndCompanion.Application      use-case interfaces + the dice expression parser
  DndCompanion.Infrastructure   EF Core + SQLite, dice roller, SRD import stub, DI
  DndCompanion.UI               Blazor Razor Class Library (reusable across hosts)
  DndCompanion.Host             ASP.NET Core + Electron.NET shell
tests/
  DndCompanion.Tests           xUnit — dice parsing/rolling
```

## Build & test
```bash
dotnet build
dotnet test
```

## Run (single command — ASP.NET Core + Electron window)
```bash
dotnet tool install ElectronNET.CLI -g   # once
cd src/DndCompanion.Host
electronize start
```
`electronize start` builds the Host, launches Kestrel, and opens the Electron
window pointed at it — one command, one process tree. First run installs
`node_modules` under `obj/Host` (a few seconds); subsequent runs are fast.

If you only want the web app (no Electron shell), e.g. for quick UI iteration
in a browser:
```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/DndCompanion.Host
```
The `ASPNETCORE_ENVIRONMENT=Development` matters: ASP.NET Core only serves
Razor Class Library / framework static web assets (`_framework/blazor.web.js`,
so *all* interactivity) from the dev-time virtual file provider in the
Development environment. Outside Development it expects those assets to have
been physically composed into `wwwroot` by `dotnet publish` — which is exactly
what `electronize start` does, so that path isn't affected. Plain
`dotnet run` without the env var boots in Production and silently 404s on
`blazor.web.js`, i.e. the page loads but nothing is clickable.

### If the Electron window shows nothing
If you're running from a terminal spawned by an Electron-based host (VS Code's
integrated terminal, some IDE extensions), it may set `ELECTRON_RUN_AS_NODE=1`
in the environment, which makes any child Electron process boot as plain
Node instead of launching its GUI — `electronize start` will then crash with
`TypeError: Cannot read properties of undefined (reading 'commandLine')`.
Unset it for the run:
```bash
env -u ELECTRON_RUN_AS_NODE electronize start
```
This is an artifact of the launching terminal, not of Electron.NET or .NET 10.

## SRD 5.2 reference data
`/data/{spells,items,actions}.json` (321 spells, 157 items, 13 actions) is
parsed by `SrdImporter` and seeded into SQLite on every Host startup — it's a
no-op once a Srd `ContentSource` row exists, so it's safe to leave unconditional.
Every row is tagged with that `ContentSource` (`ContentKind.Srd` + the CC-BY-4.0
attribution string), keeping it legally separable from any future PHB/Homebrew
rows. The JSON was parsed directly from the official SRD 5.2 text (via
5e24srd.com, itself sourced from `media.dndbeyond.com/.../SRD_CC_v5.2.pdf`) —
not hand-transcribed — so field values should match the source document. Drop
more correctly-shaped rows into those files to extend the corpus; no importer
code changes needed.

Browse it at `/spells`, `/items`, `/actions` — search box + filter chips, a
dense list, and a detail panel on row click.

## EF Core migrations
The `Initial` migration + local SQLite DB (`dndcompanion.db`, next to the built
Host) are already created. To add a new migration after changing entities:
```bash
dotnet ef migrations add <Name> -p src/DndCompanion.Infrastructure -s src/DndCompanion.Host
dotnet ef database update       -p src/DndCompanion.Infrastructure -s src/DndCompanion.Host
```

## Build order (see the full plan doc)
1. **Reference + shell** — SRD import, browse rules/spells/items  ← start here
2. **Campaigns & characters** — CRUD, character sheet, homebrew entry (Aasimar)
3. **Play view + dice** — actions/equipment/spells + dice roller + action log
4. **Audio: transcript** — NAudio → Whisper.net → live transcript
5. **Audio: structuring** — Ollama drafts notes you confirm

## Two things baked into the design
- **Rules are data, not code.** Every rules row carries a `ContentSource`
  (SRD / PHB / Homebrew). Ship only **SRD 5.2** content; `Aasimar` and homebrew
  are user-entered rows so you never redistribute protected text.
- **Audio is assistive.** Transcription is reliable; who-did-what / initiative
  position is not. Track initiative as explicit app state you tap; let audio
  annotate it. AI notes are always drafts until you confirm them.

## Heads-up on Electron.NET
Electron.NET 23.6.2 works fine against .NET 10 — no SDK-compatibility issues
found. The one real bug was in this scaffold's `Program.cs`: it created the
Electron window before calling `app.Run()`, so the window's first page load
raced Kestrel's startup and usually lost (`ERR_CONNECTION_REFUSED`). Fixed by
awaiting `app.StartAsync()` before `CreateWindowAsync` and using
`app.WaitForShutdownAsync()` in place of `app.Run()`. If Electron.NET ever
does become a blocker, the same `DndCompanion.UI` RCL drops unchanged into a
**Photino.NET** or **MAUI Blazor Hybrid** host (that's the point of the RCL).

## Attribution (required for shipped SRD content)
> This work includes material from the System Reference Document 5.2 ("SRD 5.2")
> by Wizards of the Coast LLC, available under the Creative Commons Attribution
> 4.0 International License.
