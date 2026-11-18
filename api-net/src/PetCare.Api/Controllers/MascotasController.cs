using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetCare.Api.Infraestructura;
using PetCare.Application.Contratos;
using PetCare.Application.Servicios;
using PetCare.Domain;

namespace PetCare.Api.Controllers;

[ApiController]
[Route("api/mascotas")]
[Authorize]
public class MascotasController(MascotasService mascotas, IValidator<MascotaRequest> vMascota,
    IValidator<AplicacionVacunaRequest> vVacuna) : ControladorBase
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] int page = 1, [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var (p, l) = ValidarPaginacion(page, limit);
        return Lista(await mascotas.ListarAsync(Actor, p, l, ct));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Dueno)]
    public async Task<IActionResult> Crear([FromBody] MascotaRequest r, CancellationToken ct)
    {
        await ValidarAsync(vMascota, r);
        return Respuesta(await mascotas.CrearAsync(Actor, r, ct), StatusCodes.Status201Created);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct) =>
        Respuesta(await mascotas.ObtenerAsync(Actor, id, ct));

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Dueno)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] MascotaRequest r, CancellationToken ct)
    {
        await ValidarAsync(vMascota, r);
        return Respuesta(await mascotas.ActualizarAsync(Actor, id, r, ct));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Dueno)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct) =>
        Respuesta(await mascotas.EliminarAsync(Actor, id, ct));

    [HttpPost("{id:int}/foto")]
    [Authorize(Roles = Roles.Dueno)]
    public async Task<IActionResult> SubirFoto(int id, IFormFile? foto, CancellationToken ct)
    {
        var (contenido, extension) = ImagenDe(foto);
        await using var stream = contenido;
        return Respuesta(await mascotas.ActualizarFotoAsync(Actor, id, stream, extension, ct));
    }

    [HttpGet("{id:int}/carne")]
    public async Task<IActionResult> Carne(int id, CancellationToken ct) =>
        Respuesta(await mascotas.CarneAsync(Actor, id, ct));

    [HttpGet("{id:int}/historial")]
    public async Task<IActionResult> Historial(int id, CancellationToken ct) =>
        Respuesta(await mascotas.HistorialAsync(Actor, id, ct));

    [HttpPost("{id:int}/vacunas")]
    [Authorize(Roles = Roles.Veterinario)]
    public async Task<IActionResult> RegistrarVacuna(int id, [FromBody] AplicacionVacunaRequest r, CancellationToken ct)
    {
        await ValidarAsync(vVacuna, r);
        return Respuesta(await mascotas.RegistrarVacunaAsync(Actor, id, r, ct), StatusCodes.Status201Created);
    }
}
