using System.Text.Json.Serialization;
using ParcialProgramacion.Models;

namespace ParcialProgramacion.Models.Operaciones;

/// <summary>
/// Proyeccion enviada al indice de Algolia. El objectID es el Id de la incidencia en SQLite.
/// </summary>
public class IncidenciaAlgoliaRecord
{
    [JsonPropertyName("objectID")]
    public string ObjectID { get; set; } = string.Empty;

    public int Id { get; set; }

    public string Estacion { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public static IncidenciaAlgoliaRecord From(Incidencia incidencia) => new()
    {
        ObjectID = incidencia.Id.ToString(),
        Id = incidencia.Id,
        Estacion = incidencia.Estacion,
        Descripcion = incidencia.Descripcion
    };
}
