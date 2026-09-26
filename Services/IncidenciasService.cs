using Microsoft.EntityFrameworkCore;
using ParcialProgramacion.Data;
using ParcialProgramacion.Models;
using ParcialProgramacion.Models.Operaciones;

namespace ParcialProgramacion.Services;

public class IncidenciasService : IIncidenciasService
{
    private readonly ApplicationDbContext _context;
    private readonly IIncidenciasNotificador _notificador;
    private readonly ILogger<IncidenciasService> _logger;

    public IncidenciasService(
        ApplicationDbContext context,
        IIncidenciasNotificador notificador,
        ILogger<IncidenciasService> logger)
    {
        _context = context;
        _notificador = notificador;
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

        // 1) Primero se guarda el estado en la base de datos.
        await _context.SaveChangesAsync();

        _logger.LogInformation("Incidencia {Id} cerrada en BD", id);

        // 2) Solo despues de persistir se publica el evento en tiempo real.
        await _notificador.NotificarActualizadaAsync(new IncidenciaActualizadaDto
        {
            Id = incidencia.Id,
            Estado = incidencia.Estado.ToString()
        });

        return true;
    }
}
