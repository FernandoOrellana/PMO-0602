using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetCare.Api.Infraestructura;
using PetCare.Application.Contratos;
using PetCare.Application.Servicios;
using PetCare.Domain;

namespace PetCare.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = Roles.Administrador)]
public class CobrosController(CobrosService cobros) : ControladorBase
{
    [HttpPost("pedidos/{id:int}/cobro")]
    public async Task<IActionResult> CobrarPedido(int id, [FromBody] RegistrarCobroRequest request, CancellationToken ct) =>
        Resultado(await cobros.CobrarPedidoAsync(Actor, id, request, ct));

    [HttpPost("citas/{id:int}/cobro")]
    public async Task<IActionResult> CobrarCita(int id, [FromBody] RegistrarCobroRequest request, CancellationToken ct) =>
        Resultado(await cobros.CobrarCitaAsync(Actor, id, request, ct));

    [HttpGet("pedidos/{id:int}/cobro")]
    public async Task<IActionResult> CobroPedido(int id, CancellationToken ct) =>
        Respuesta(await cobros.ObtenerPorOrigenAsync(Actor, true, id, ct));

    [HttpGet("citas/{id:int}/cobro")]
    public async Task<IActionResult> CobroCita(int id, CancellationToken ct) =>
        Respuesta(await cobros.ObtenerPorOrigenAsync(Actor, false, id, ct));

    [HttpGet("cobros/{id:int}")]
    public async Task<IActionResult> Comprobante(int id, CancellationToken ct) =>
        Respuesta(await cobros.ObtenerAsync(Actor, id, ct));

    private IActionResult Resultado(ResultadoCobro resultado) =>
        Respuesta(resultado.Comprobante, resultado.Creado ? StatusCodes.Status201Created : StatusCodes.Status200OK);
}
