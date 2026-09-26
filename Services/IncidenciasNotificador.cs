using Microsoft.AspNetCore.SignalR;
using ParcialProgramacion.Hubs;
using ParcialProgramacion.Models.Operaciones;

namespace ParcialProgramacion.Services;

public class IncidenciasNotificador : IIncidenciasNotificador
{
    private readonly IHubContext<IncidenciasHub> _hub;
    private readonly ILogger<IncidenciasNotificador> _logger;

    public IncidenciasNotificador(IHubContext<IncidenciasHub> hub, ILogger<IncidenciasNotificador> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task NotificarActualizadaAsync(IncidenciaActualizadaDto dto)
    {
        await _hub.Clients.All.SendAsync(IncidenciasHub.EventoIncidenciaActualizada, dto);

        _logger.LogInformation(
            "Evento {Evento} publicado por SignalR: Id={Id} Estado={Estado}",
            IncidenciasHub.EventoIncidenciaActualizada, dto.Id, dto.Estado);
    }
}
