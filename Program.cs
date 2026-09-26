using Microsoft.EntityFrameworkCore;
using ParcialProgramacion.Configuration;
using ParcialProgramacion.Data;
using ParcialProgramacion.Hubs;
using ParcialProgramacion.Services;

var builder = WebApplication.CreateBuilder(args);

// Render asigna el puerto de escucha mediante la variable de entorno PORT.
// Kestrel debe enlazar a 0.0.0.0 para que el proxy de Render alcance la app.
// Fuera de Render (dotnet run local) se respeta ASPNETCORE_HTTP_PORTS/launchSettings.
if (int.TryParse(builder.Configuration["PORT"], out var puertoRender) && puertoRender > 0)
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{puertoRender}");
}

builder.Services.AddControllersWithViews();

builder.Services.AddSignalR();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("IncidenciasDb")));

builder.Services.AddSingleton<IAlgoliaService, AlgoliaService>();

builder.Services.AddScoped<IIncidenciasNotificador, IncidenciasNotificador>();

builder.Services.AddScoped<IIncidenciasService, IncidenciasService>();

// La cadena de Redis se resuelve desde variables de entorno o configuracion externa.
// Nunca se escribe la contrasena en appsettings.json porque ese archivo se versiona en Git.
// Orden de precedencia: Redis:ConnectionString, RedisConnectionString,
// ConnectionStrings:Redis, REDIS_CONNECTION, REDIS_URL.
var redisConnectionString = new[]
{
    builder.Configuration["Redis:ConnectionString"],
    builder.Configuration["RedisConnectionString"],
    builder.Configuration.GetConnectionString("Redis"),
    builder.Configuration["REDIS_CONNECTION"],
    builder.Configuration["REDIS_URL"]
}
.FirstOrDefault(valor => !string.IsNullOrWhiteSpace(valor));

if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = RedisConfiguration.Normalizar(redisConnectionString);
        options.InstanceName = "ExamenParcial_";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Operaciones/Incidencias");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Operaciones}/{action=Incidencias}/{id?}");

app.MapHub<IncidenciasHub>(IncidenciasHub.Ruta);

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var dataSource = context.Database.GetDbConnection().DataSource;

    if (!string.IsNullOrWhiteSpace(dataSource) && dataSource != ":memory:")
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    context.Database.EnsureCreated();

    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation(
        "Cache distribuido: {Proveedor}",
        string.IsNullOrWhiteSpace(redisConnectionString) ? "memoria (REDIS_CONNECTION no configurada)" : "Redis");

    var algoliaService = scope.ServiceProvider.GetRequiredService<IAlgoliaService>();

    await algoliaService.IndexarAsync(await context.Incidencias.ToListAsync());
}

app.Run();
