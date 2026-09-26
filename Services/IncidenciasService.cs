using Microsoft.EntityFrameworkCore;
using ParcialProgramacion.Data;
using ParcialProgramacion.Models;

namespace ParcialProgramacion.Services;

public class IncidenciasService : IIncidenciasService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<IncidenciasService> _logger;

    public IncidenciasService(ApplicationDbContext context, ILogger<IncidenciasService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Incidencia>> GetAbiertasAsync()
    {
        _logger.LogInformation("Origen de lectura: BD");

        return await _context.Incidencias
            .Where(i => i.Estado == EstadoIncidencia.Abierta)
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

        await _context.SaveChangesAsync();

        _logger.LogInformation("Incidencia {Id} cerrada en BD", id);

        return true;
    }
}
