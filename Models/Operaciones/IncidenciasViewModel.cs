using ParcialProgramacion.Models;

namespace ParcialProgramacion.Models.Operaciones;

public class IncidenciasViewModel
{
    public List<Incidencia> Incidencias { get; set; } = new();

    public string TextoBusqueda { get; set; } = string.Empty;

    public bool HayBusqueda => !string.IsNullOrWhiteSpace(TextoBusqueda);
}
