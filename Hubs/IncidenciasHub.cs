using Microsoft.AspNetCore.SignalR;

namespace ParcialProgramacion.Hubs;

/// <summary>
/// Hub de tiempo real para la pantalla de incidencias.
/// Las vistas se suscriben y reciben el evento IncidenciaActualizada con Id y Estado.
/// </summary>
public class IncidenciasHub : Hub
{
    public const string Ruta = "/hubs/incidencias";
    public const string EventoIncidenciaActualizada = "IncidenciaActualizada";

    public Task Suscribir() => Groups.AddToGroupAsync(Context.ConnectionId, "operaciones");
}
