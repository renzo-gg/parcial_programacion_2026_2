using Algolia.Search.Clients;
using Algolia.Search.Exceptions;
using Algolia.Search.Models.Ingestion;
using Algolia.Search.Models.Search;
using ParcialProgramacion.Models;
using ParcialProgramacion.Models.Operaciones;

namespace ParcialProgramacion.Services;

public class AlgoliaService : IAlgoliaService
{
    private const int HitsPorPagina = 1000;

    private readonly IConfiguration _configuration;
    private readonly ILogger<AlgoliaService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Lock _candado = new();

    private List<IncidenciaAlgoliaRecord> _indiceLocal = new();
    private SearchClient? _cliente;
    private bool _configurado;

    public AlgoliaService(
        IConfiguration configuration,
        ILogger<AlgoliaService> logger,
        ILoggerFactory loggerFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    public string NombreIndice =>
        Leer("Algolia:IndexName", "ALGOLIA_INDEX_NAME") ?? "incidencias";

    public async Task IndexarAsync(IEnumerable<Incidencia> incidencias, CancellationToken cancellationToken = default)
    {
        var registros = incidencias.Select(IncidenciaAlgoliaRecord.From).ToList();

        lock (_candado)
        {
            _indiceLocal = registros;
        }

        var cliente = ObtenerCliente();

        if (cliente is null)
        {
            return;
        }

        try
        {
            await cliente.SetSettingsAsync(NombreIndice, new IndexSettings
            {
                SearchableAttributes = new List<string> { nameof(IncidenciaAlgoliaRecord.Estacion), nameof(IncidenciaAlgoliaRecord.Descripcion) },
                AttributesToRetrieve = new List<string>
                {
                    nameof(IncidenciaAlgoliaRecord.ObjectID),
                    nameof(IncidenciaAlgoliaRecord.Id),
                    nameof(IncidenciaAlgoliaRecord.Estacion),
                    nameof(IncidenciaAlgoliaRecord.Descripcion)
                }
            });

            await cliente.SaveObjectsAsync(NombreIndice, registros);

            _logger.LogInformation("Indice Algolia '{Indice}' cargado con {Cantidad} registros", NombreIndice, registros.Count);
        }
        catch (AlgoliaException ex)
        {
            _logger.LogError(ex, "No se pudo cargar el indice de Algolia '{Indice}'", NombreIndice);
        }
    }

    public async Task<List<int>> BuscarIdsAsync(string texto, CancellationToken cancellationToken = default)
    {
        var consulta = texto?.Trim() ?? string.Empty;

        if (consulta.Length == 0)
        {
            return new List<int>();
        }

        var cliente = ObtenerCliente();

        if (cliente is null)
        {
            List<IncidenciaAlgoliaRecord> indice;
            lock (_candado)
            {
                indice = _indiceLocal;
            }

            var idsLocales = indice
                .Where(r => r.Estacion.Contains(consulta, StringComparison.OrdinalIgnoreCase)
                         || r.Descripcion.Contains(consulta, StringComparison.OrdinalIgnoreCase))
                .Select(r => r.Id)
                .ToList();

            _logger.LogInformation("Busqueda '{Consulta}' resuelta en el indice local: {Cantidad} coincidencias", consulta, idsLocales.Count);

            return idsLocales;
        }

        try
        {
            var parametros = new SearchParams(new SearchParamsObject
            {
                Query = consulta,
                HitsPerPage = HitsPorPagina
            });

            var respuesta = await cliente.SearchSingleIndexAsync<IncidenciaAlgoliaRecord>(NombreIndice, parametros);

            var ids = respuesta.Hits
                .Select(h => h.Id)
                .Where(id => id > 0)
                .ToList();

            _logger.LogInformation("Busqueda '{Consulta}' en Algolia: {Cantidad} coincidencias", consulta, ids.Count);

            return ids;
        }
        catch (AlgoliaException ex)
        {
            _logger.LogError(ex, "Error consultando Algolia para '{Consulta}'", consulta);
            return new List<int>();
        }
    }

    private SearchClient? ObtenerCliente()
    {
        if (_configurado)
        {
            return _cliente;
        }

        var appId = Leer("Algolia:AppId", "ALGOLIA_APP_ID");
        var adminApiKey = Leer("Algolia:AdminApiKey", "ALGOLIA_ADMIN_API_KEY");

        if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(adminApiKey))
        {
            _logger.LogWarning(
                "ALGOLIA_APP_ID / ALGOLIA_ADMIN_API_KEY no configurados: la busqueda usara el indice en memoria. Definalas como variables de entorno en Render.");

            _configurado = true;
            return null;
        }

        _cliente = new SearchClient(appId, adminApiKey, _loggerFactory);
        _configurado = true;

        return _cliente;
    }

    private string? Leer(string claveConfiguracion, string variableEntorno) =>
        _configuration[claveConfiguracion] ?? _configuration[variableEntorno];
}
