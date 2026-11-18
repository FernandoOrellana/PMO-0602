using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetCare.Api.Infraestructura;
using PetCare.Application.Servicios;

namespace PetCare.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class CatalogosController(CatalogosService catalogos) : ControladorBase
{
    [HttpGet("especies")]
    public async Task<IActionResult> Especies(CancellationToken ct) => Respuesta(await catalogos.EspeciesAsync(ct));

    [HttpGet("vacunas")]
    public async Task<IActionResult> Vacunas(CancellationToken ct) => Respuesta(await catalogos.VacunasAsync(ct));

    [HttpGet("servicios")]
    public async Task<IActionResult> Servicios(CancellationToken ct) => Respuesta(await catalogos.ServiciosAsync(ct));

    [HttpGet("veterinarios")]
    public async Task<IActionResult> Veterinarios(CancellationToken ct) => Respuesta(await catalogos.VeterinariosAsync(ct));

    [HttpGet("veterinarios/{id:int}/disponibilidad")]
    public async Task<IActionResult> Disponibilidad(int id, [FromQuery] DateOnly fecha, [FromQuery] int? servicioId, CancellationToken ct) =>
        Respuesta(await catalogos.DisponibilidadAsync(id, fecha, servicioId, ct));
}
