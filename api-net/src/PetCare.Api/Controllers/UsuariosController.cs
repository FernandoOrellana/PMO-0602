using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetCare.Api.Infraestructura;
using PetCare.Application.Contratos;
using PetCare.Application.Servicios;

namespace PetCare.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class UsuariosController(NotificacionesService notificaciones, IValidator<DispositivoRequest> vDispositivo) : ControladorBase
{
    [HttpPost("usuarios/me/dispositivos")]
    public async Task<IActionResult> RegistrarDispositivo([FromBody] DispositivoRequest r, CancellationToken ct)
    {
        await ValidarAsync(vDispositivo, r);
        return Respuesta(await notificaciones.RegistrarDispositivoAsync(Actor.Id, r.FcmToken, ct));
    }

    [HttpGet("notificaciones")]
    public async Task<IActionResult> Listar(CancellationToken ct) =>
        Respuesta(await notificaciones.ListarAsync(Actor.Id, ct));

    [HttpPatch("notificaciones/{id:int}/leer")]
    public async Task<IActionResult> Leer(int id, CancellationToken ct) =>
        Respuesta(await notificaciones.MarcarLeidaAsync(Actor.Id, id, ct));

    [HttpPost("notificaciones/prueba")]
    public async Task<IActionResult> Probar(CancellationToken ct) =>
        Respuesta(await notificaciones.ProbarAsync(Actor.Id, ct));
}
