using Microsoft.EntityFrameworkCore;
using ParcialProgramacion.Data;
using ParcialProgramacion.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("IncidenciasDb")));

builder.Services.AddScoped<IIncidenciasService, IncidenciasService>();

var redisConnectionString = builder.Configuration["Redis:ConnectionString"]
    ?? builder.Configuration["REDIS_CONNECTION"];

if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "parcial:";
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
}

app.Run();
