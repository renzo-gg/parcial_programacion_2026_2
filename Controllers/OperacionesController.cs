using Microsoft.AspNetCore.Mvc;
using ParcialProgramacion.Models.Operaciones;
using ParcialProgramacion.Services;

namespace ParcialProgramacion.Controllers;

public class OperacionesController : Controller
{
    private readonly IIncidenciasService _incidenciasService;

    public OperacionesController(IIncidenciasService incidenciasService)
    {
        _incidenciasService = incidenciasService;
    }

    [HttpGet]
    public async Task<IActionResult> Incidencias()
    {
        var model = new IncidenciasViewModel
        {
            Incidencias = await _incidenciasService.GetAbiertasAsync()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cerrar(int id)
    {
        await _incidenciasService.CerrarAsync(id);

        return RedirectToAction(nameof(Incidencias));
    }
}
