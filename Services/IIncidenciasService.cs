using ParcialProgramacion.Models;

namespace ParcialProgramacion.Services;

public interface IIncidenciasService
{
    Task<List<Incidencia>> GetAbiertasAsync();

    Task<List<Incidencia>> BuscarAsync(string? texto);

    Task<bool> CerrarAsync(int id);
}
