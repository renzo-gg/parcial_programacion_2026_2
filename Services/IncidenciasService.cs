using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using ParcialProgramacion.Data;
using ParcialProgramacion.Models;
using ParcialProgramacion.Models.Operaciones;

namespace ParcialProgramacion.Services;

public class IncidenciasService : IIncidenciasService
{
    public const string ClaveListadoAbiertas = "operaciones:incidencias:abiertas";

    private static readonly TimeSpan ExpiracionListado = TimeSpan.FromSeconds(60);

    private readonly ApplicationDbContext _context;
    private readonly IAlgoliaService _algoliaService;
    private readonly IDistributedCache _cache;
    private readonly IIncidenciasNotificador _notificador;
    private readonly ILogger<IncidenciasService> _logger;

    public IncidenciasService(
        ApplicationDbContext context,
        IAlgoliaService algoliaService,
        IDistributedCache cache,
        IIncidenciasNotificador notificador,
        ILogger<IncidenciasService> logger)
    {
        _context = context;
        _algoliaService = algoliaService;
        _cache = cache;
        _notificador = notificador;
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

    public async Task<List<Incidencia>> BuscarAsync(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return await GetAbiertasAsync();
        }

        _logger.LogInformation("Busqueda con termino: omite la cache de Redis y consulta Algolia + Base de Datos");

        var ids = await _algoliaService.BuscarIdsAsync(texto);

        if (ids.Count == 0)
        {
            return new List<Incidencia>();
        }

        return await _context.Incidencias
            .Where(i => ids.Contains(i.Id) && i.Estado == EstadoIncidencia.Abierta)
            .OrderBy(i => i.FechaApertura)
            .ToListAsync();
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

        // 1) Primero se guarda el estado en la base de datos.
        await _context.SaveChangesAsync();

        _logger.LogInformation("Incidencia {Id} cerrada en BD", id);

        await InvalidarCacheListadoAsync();

        // 2) Solo despues de persistir se publica el evento en tiempo real.
        await _notificador.NotificarActualizadaAsync(new IncidenciaActualizadaDto
        {
            Id = incidencia.Id,
            Estado = incidencia.Estado.ToString()
        });

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
