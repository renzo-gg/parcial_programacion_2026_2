using System.ComponentModel.DataAnnotations;

namespace ParcialProgramacion.Models;

public enum EstadoIncidencia
{
    Abierta = 1,
    Cerrada = 2
}

public class Incidencia
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Estacion { get; set; } = string.Empty;

    [Required, MaxLength(240)]
    public string Descripcion { get; set; } = string.Empty;

    public EstadoIncidencia Estado { get; set; } = EstadoIncidencia.Abierta;

    public DateTime FechaApertura { get; set; } = DateTime.UtcNow;

    public DateTime? FechaCierre { get; set; }
}
