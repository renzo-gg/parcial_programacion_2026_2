using ParcialProgramacion.Models;

namespace ParcialProgramacion.Services;

public interface IIncidenciasService
{
    Task<List<Incidencia>> GetAbiertasAsync();

    Task<bool> CerrarAsync(int id);
}
