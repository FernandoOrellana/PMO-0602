using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetCare.Api.Infraestructura;
using PetCare.Application.Abstracciones;
using PetCare.Application.Contratos;
using PetCare.Application.Servicios;
using PetCare.Domain;

namespace PetCare.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = Roles.Administrador)]
public class AdminController(
    AdminService admin,
    IValidator<ProductoRequest> vProducto,
    IValidator<ServicioRequest> vServicio,
    IValidator<VeterinarioCrearRequest> vVetCrear,
    IValidator<VeterinarioActualizarRequest> vVetActualizar,
    IValidator<IReadOnlyList<HorarioRequest>> vHorarios) : ControladorBase
{
    [HttpPost("productos")]
    public async Task<IActionResult> CrearProducto([FromBody] ProductoRequest r, CancellationToken ct)
    {
        await ValidarAsync(vProducto, r);
        return Respuesta(await admin.CrearProductoAsync(r, ct), StatusCodes.Status201Created);
    }

    [HttpPut("productos/{id:int}")]
    public async Task<IActionResult> ActualizarProducto(int id, [FromBody] ProductoRequest r, CancellationToken ct)
    {
        await ValidarAsync(vProducto, r);
        return Respuesta(await admin.ActualizarProductoAsync(id, r, ct));
    }

    [HttpDelete("productos/{id:int}")]
    public async Task<IActionResult> DesactivarProducto(int id, CancellationToken ct) =>
        Respuesta(await admin.DesactivarProductoAsync(id, ct));

    [HttpPost("productos/{id:int}/imagen")]
    public async Task<IActionResult> ImagenProducto(int id, IFormFile? imagen, CancellationToken ct)
    {
        var (contenido, extension) = ImagenDe(imagen);
        await using var stream = contenido;
        return Respuesta(await admin.ActualizarImagenProductoAsync(id, stream, extension, ct));
    }

    [HttpPost("servicios")]
    public async Task<IActionResult> CrearServicio([FromBody] ServicioRequest r, CancellationToken ct)
    {
        await ValidarAsync(vServicio, r);
        return Respuesta(await admin.CrearServicioAsync(r, ct), StatusCodes.Status201Created);
    }

    [HttpPut("servicios/{id:int}")]
    public async Task<IActionResult> ActualizarServicio(int id, [FromBody] ServicioRequest r, CancellationToken ct)
    {
        await ValidarAsync(vServicio, r);
        return Respuesta(await admin.ActualizarServicioAsync(id, r, ct));
    }

    [HttpDelete("servicios/{id:int}")]
    public async Task<IActionResult> DesactivarServicio(int id, CancellationToken ct) =>
        Respuesta(await admin.DesactivarServicioAsync(id, ct));

    [HttpPost("veterinarios")]
    public async Task<IActionResult> CrearVeterinario([FromBody] VeterinarioCrearRequest r, CancellationToken ct)
    {
        await ValidarAsync(vVetCrear, r);
        return Respuesta(await admin.CrearVeterinarioAsync(r, ct), StatusCodes.Status201Created);
    }

    [HttpPut("veterinarios/{id:int}")]
    public async Task<IActionResult> ActualizarVeterinario(int id, [FromBody] VeterinarioActualizarRequest r, CancellationToken ct)
    {
        await ValidarAsync(vVetActualizar, r);
        return Respuesta(await admin.ActualizarVeterinarioAsync(id, r, ct));
    }

    [HttpPut("veterinarios/{id:int}/horarios")]
    public async Task<IActionResult> Horarios(int id, [FromBody] IReadOnlyList<HorarioRequest> r, CancellationToken ct)
    {
        await ValidarAsync(vHorarios, r);
        return Respuesta(await admin.ReemplazarHorariosAsync(id, r, ct));
    }

    [HttpGet("reportes/ventas")]
    public async Task<IActionResult> Ventas([FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken ct) =>
        Respuesta(await admin.ReporteVentasAsync(desde, hasta, ct));

    [HttpGet("reportes/citas")]
    public async Task<IActionResult> Citas([FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken ct) =>
        Respuesta(await admin.ReporteCitasAsync(desde, hasta, ct));
}
