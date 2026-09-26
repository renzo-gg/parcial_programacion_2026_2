using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using ParcialProgramacion.Data;
using ParcialProgramacion.Models;

namespace ParcialProgramacion.Services;

public class IncidenciasService : IIncidenciasService
{
    public const string ClaveListadoAbiertas = "operaciones:incidencias:abiertas";

    private static readonly TimeSpan ExpiracionListado = TimeSpan.FromSeconds(60);

    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<IncidenciasService> _logger;

    public IncidenciasService(
        ApplicationDbContext context,
        IDistributedCache cache,
        ILogger<IncidenciasService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<Incidencia>> GetAbiertasAsync()
    {
        var cacheado = await _cache.GetAsync(ClaveListadoAbiertas);

        if (cacheado is { Length: > 0 })
        {
            var desdeCache = JsonSerializer.Deserialize<List<Incidencia>>(cacheado);

            if (desdeCache is not null)
            {
                _logger.LogInformation("Origen de lectura: Redis");

                return desdeCache;
            }
        }

        _logger.LogInformation("Origen de lectura: Base de Datos");

        var listado = await ObtenerAbiertasDesdeBaseDeDatosAsync();

        await _cache.SetAsync(
            ClaveListadoAbiertas,
            JsonSerializer.SerializeToUtf8Bytes(listado),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ExpiracionListado });

        return listado;
    }

    public async Task<bool> CerrarAsync(int id)
    {
        var incidencia = await _context.Incidencias.FirstOrDefaultAsync(i => i.Id == id);

        if (incidencia is null || incidencia.Estado == EstadoIncidencia.Cerrada)
        {
            return false;
        }

        incidencia.Estado = EstadoIncidencia.Cerrada;
        incidencia.FechaCierre = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Incidencia {Id} cerrada en BD", id);

        await InvalidarCacheListadoAsync();

        return true;
    }

    private async Task<List<Incidencia>> ObtenerAbiertasDesdeBaseDeDatosAsync() =>
        await _context.Incidencias
            .Where(i => i.Estado == EstadoIncidencia.Abierta)
            .OrderBy(i => i.FechaApertura)
            .ToListAsync();

    private async Task InvalidarCacheListadoAsync()
    {
        await _cache.RemoveAsync(ClaveListadoAbiertas);

        _logger.LogInformation("Cache invalidada: {Clave}", ClaveListadoAbiertas);
    }
}
