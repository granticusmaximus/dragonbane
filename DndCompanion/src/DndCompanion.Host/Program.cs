using DndCompanion.Host.Components;
using DndCompanion.Infrastructure;
using DndCompanion.Infrastructure.Dev;
using DndCompanion.Infrastructure.Persistence;
using DndCompanion.Infrastructure.Srd;
using DndCompanion.UI.Components;
using ElectronNET.API;
using ElectronNET.API.Entities;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Electron.NET: wires the ASP.NET Core host into an Electron window.
builder.WebHost.UseElectron(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Stable per-user app data dir — NOT AppContext.BaseDirectory, which is the build output
// folder and differs between `dotnet run` (bin/Debug/net10.0) and `electronize start`
// (obj/Host/bin). DB, downloaded Whisper model, and recordings all live here so every
// launch method sees the same data.
var appDataDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "DndCompanion");
builder.Services.AddInfrastructure(appDataDir);

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<DndCompanion.Host.Components.App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(DiceRoller).Assembly);

// Local-first desktop app: apply pending migrations and seed SRD content on every
// startup rather than requiring a separate `dotnet ef database update` step. Both are
// no-ops once the DB is current / already seeded.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    var importer = scope.ServiceProvider.GetRequiredService<SrdImporter>();
    await importer.ImportAsync(Path.Combine(AppContext.BaseDirectory, "data"));

    // Dev-only fixture data — never runs outside Development, never ships in a packaged build.
    if (app.Environment.IsDevelopment())
        await DevSeed.BaloneySlimAsync(db);
}

// Start Kestrel before creating the Electron window — CreateWindowAsync triggers an
// immediate page load, and racing it against app.Run() causes an ERR_CONNECTION_REFUSED
// on first launch (the window loads before the ASP.NET Core host is listening).
await app.StartAsync();

if (HybridSupport.IsElectronActive)
{
    await Electron.WindowManager.CreateWindowAsync(new BrowserWindowOptions
    {
        Width = 1280,
        Height = 800,
        Title = "D&D Companion"
    });
}

await app.WaitForShutdownAsync();
