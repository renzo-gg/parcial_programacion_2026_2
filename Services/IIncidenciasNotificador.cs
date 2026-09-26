using ParcialProgramacion.Models.Operaciones;

namespace ParcialProgramacion.Services;

public interface IIncidenciasNotificador
{
    Task NotificarActualizadaAsync(IncidenciaActualizadaDto dto);
}
