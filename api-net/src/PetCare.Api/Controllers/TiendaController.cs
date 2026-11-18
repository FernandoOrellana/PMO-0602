using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetCare.Api.Infraestructura;
using PetCare.Application.Contratos;
using PetCare.Application.Servicios;
using PetCare.Domain;

namespace PetCare.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class TiendaController(TiendaService tienda, IValidator<PedidoRequest> vPedido) : ControladorBase
{
    [HttpGet("productos")]
    public async Task<IActionResult> Productos([FromQuery] string? categoria, [FromQuery] int page = 1,
        [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var (p, l) = ValidarPaginacion(page, limit);
        return Lista(await tienda.ListarProductosAsync(categoria, p, l, ct));
    }

    [HttpGet("productos/{id:int}")]
    public async Task<IActionResult> Producto(int id, CancellationToken ct) =>
        Respuesta(await tienda.ObtenerProductoAsync(id, ct));

    [HttpPost("pedidos")]
    [Authorize(Roles = Roles.Dueno)]
    public async Task<IActionResult> CrearPedido([FromBody] PedidoRequest r, CancellationToken ct)
    {
        await ValidarAsync(vPedido, r);
        return Respuesta(await tienda.CrearPedidoAsync(Actor, r, ct), StatusCodes.Status201Created);
    }

    [HttpGet("pedidos")]
    [Authorize(Roles = Roles.Dueno + "," + Roles.Administrador)]
    public async Task<IActionResult> Pedidos([FromQuery] int page = 1, [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var (p, l) = ValidarPaginacion(page, limit);
        return Lista(await tienda.ListarPedidosAsync(Actor, p, l, ct));
    }

    [HttpGet("pedidos/{id:int}")]
    [Authorize(Roles = Roles.Dueno + "," + Roles.Administrador)]
    public async Task<IActionResult> Pedido(int id, CancellationToken ct) =>
        Respuesta(await tienda.ObtenerPedidoAsync(Actor, id, ct));

    [HttpPatch("pedidos/{id:int}/estado")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] EstadoPedidoRequest r, CancellationToken ct) =>
        Respuesta(await tienda.CambiarEstadoAsync(id, r.Estado, ct));
}
