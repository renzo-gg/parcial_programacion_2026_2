namespace ParcialProgramacion.Services;

public interface IAlgoliaService
{
    Task IndexarAsync(IEnumerable<Models.Incidencia> incidencias, CancellationToken cancellationToken = default);

    Task<List<int>> BuscarIdsAsync(string texto, CancellationToken cancellationToken = default);
}
