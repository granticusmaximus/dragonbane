using DndCompanion.Host.Components;
using DndCompanion.Infrastructure;
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

// SQLite lives next to the app data — swap for a per-user path in production.
var dbPath = Path.Combine(AppContext.BaseDirectory, "dndcompanion.db");
builder.Services.AddInfrastructure(dbPath);

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
