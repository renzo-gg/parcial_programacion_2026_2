namespace ParcialProgramacion.Models.Operaciones;

/// <summary>
/// Evento publicado por el Hub cuando una incidencia cambia de estado.
/// </summary>
public class IncidenciaActualizadaDto
{
    public int Id { get; set; }

    public string Estado { get; set; } = string.Empty;
}
