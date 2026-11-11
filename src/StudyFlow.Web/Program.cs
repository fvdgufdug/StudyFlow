using Microsoft.EntityFrameworkCore;
using StudyFlow.Application.Contracts;
using StudyFlow.Application.Services;
using StudyFlow.Infrastructure;
using StudyFlow.Web.Components;

System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
var databasePath = builder.Configuration["StudyFlow:DatabasePath"] ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "studyflow.db");
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(databasePath))!);
var connection = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder { DataSource = databasePath, ForeignKeys = true };
builder.Services.AddDbContextFactory<PlannerDbContext>(options => options.UseSqlite(connection.ToString()));
builder.Services.AddScoped<IPlannerRepository, SqlitePlannerRepository>();
builder.Services.AddScoped<PlannerState>();
var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<PlannerDbContext>>();
    await using var db = await factory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
}
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.UseStaticFiles();
app.MapStaticAssets();
app.UseAntiforgery();
app.MapGet("/export", async (IPlannerRepository repository) => Results.File(
    BackupCodec.Export(await repository.LoadAsync()),
    "application/json", "studyflow.json"));
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
