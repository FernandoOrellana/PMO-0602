using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetCare.Application.Contratos;
using PetCare.Application.Servicios;
using PetCare.Api.Infraestructura;

namespace PetCare.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService auth, IValidator<RegistroRequest> vRegistro,
    IValidator<VerificarEmailRequest> vVerificar, IValidator<EmailRequest> vEmail,
    IValidator<LoginRequest> vLogin, IValidator<RefrescarRequest> vRefrescar,
    IValidator<RestablecerRequest> vRestablecer) : ControladorBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Registrar([FromBody] RegistroRequest r, CancellationToken ct)
    {
        await ValidarAsync(vRegistro, r);
        return Respuesta(await auth.RegistrarAsync(r, ct), StatusCodes.Status201Created);
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> Verificar([FromBody] VerificarEmailRequest r, CancellationToken ct)
    {
        await ValidarAsync(vVerificar, r);
        return Respuesta(await auth.VerificarEmailAsync(r, ct));
    }

    [HttpPost("resend-code")]
    public async Task<IActionResult> ReenviarCodigo([FromBody] EmailRequest r, CancellationToken ct)
    {
        await ValidarAsync(vEmail, r);
        return Respuesta(await auth.ReenviarCodigoAsync(r, ct));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest r, CancellationToken ct)
    {
        await ValidarAsync(vLogin, r);
        return Respuesta(await auth.LoginAsync(r, ct));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refrescar([FromBody] RefrescarRequest r, CancellationToken ct)
    {
        await ValidarAsync(vRefrescar, r);
        return Respuesta(await auth.RefrescarAsync(r, ct));
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> OlvidePassword([FromBody] EmailRequest r, CancellationToken ct)
    {
        await ValidarAsync(vEmail, r);
        return Respuesta(await auth.OlvidePasswordAsync(r, ct));
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> Restablecer([FromBody] RestablecerRequest r, CancellationToken ct)
    {
        await ValidarAsync(vRestablecer, r);
        return Respuesta(await auth.RestablecerPasswordAsync(r, ct));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> CerrarSesion([FromBody] RefrescarRequest r, CancellationToken ct)
    {
        await ValidarAsync(vRefrescar, r);
        return Respuesta(await auth.CerrarSesionAsync(r, ct));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct) =>
        Respuesta(await auth.PerfilAsync(Actor.Id, ct));
}
