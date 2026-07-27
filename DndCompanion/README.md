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
dotnet run --project src/DndCompanion.Host
```

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
