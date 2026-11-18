using Microsoft.Extensions.Options;
using PetCare.Application.Abstracciones;
using PetCare.Application.Common;
using PetCare.Application.Contratos;
using PetCare.Domain;

namespace PetCare.Application.Servicios;

public class AuthService(
    IUsuarioRepositorio usuarios,
    IRefreshTokenRepositorio refreshTokens,
    ISeguridad seguridad,
    INotificadorCorreo correo,
    IReloj reloj,
    IUnitOfWork uow,
    IOptions<OpcionesApp> opciones,
    Mapeador mapeador)
{
    private readonly OpcionesApp _opciones = opciones.Value;

    public async Task<MensajeDto> RegistrarAsync(RegistroRequest r, CancellationToken ct)
    {
        var email = Normalizar(r.Email);
        if (await usuarios.BuscarPorEmailAsync(email, ct) is not null)
            throw new ConflictoException("Ya existe una cuenta con ese correo");

        var codigo = seguridad.NuevoCodigo();
        usuarios.Agregar(new Usuario
        {
            RolId = await usuarios.IdRolAsync(Roles.Dueno, ct),
            Nombre = r.Nombre.Trim(),
            Email = email,
            Telefono = r.Telefono,
            PasswordHash = seguridad.HashPassword(r.Password),
            EmailVerificado = false,
            CodigoVerificacionHash = seguridad.Sha256(codigo),
            CodigoExpira = reloj.Ahora.AddMinutes(_opciones.CodigoExpiraMinutos),
            CreatedAt = reloj.Ahora,
        });
        await uow.GuardarAsync(ct);
        await correo.EnviarCodigoVerificacionAsync(email, codigo, ct);
        return new MensajeDto("Cuenta creada. Revisa tu correo para ingresar el código de verificación.");
    }

    public async Task<MensajeDto> VerificarEmailAsync(VerificarEmailRequest r, CancellationToken ct)
    {
        var usuario = await usuarios.BuscarPorEmailAsync(Normalizar(r.Email), ct)
            ?? throw new NoEncontradoException("Usuario no encontrado");
        if (usuario.EmailVerificado) return new MensajeDto("La cuenta ya está verificada");

        var valido = usuario.CodigoVerificacionHash is not null
            && usuario.CodigoExpira > reloj.Ahora
            && usuario.CodigoVerificacionHash == seguridad.Sha256(r.Codigo);
        if (!valido) throw new ValidacionException("El código es inválido o expiró");

        usuario.EmailVerificado = true;
        usuario.CodigoVerificacionHash = null;
        usuario.CodigoExpira = null;
        await uow.GuardarAsync(ct);
        return new MensajeDto("Correo verificado. Ya puedes iniciar sesión.");
    }

    public async Task<MensajeDto> ReenviarCodigoAsync(EmailRequest r, CancellationToken ct)
    {
        var usuario = await usuarios.BuscarPorEmailAsync(Normalizar(r.Email), ct);
        if (usuario is not null && !usuario.EmailVerificado)
        {
            var codigo = seguridad.NuevoCodigo();
            usuario.CodigoVerificacionHash = seguridad.Sha256(codigo);
            usuario.CodigoExpira = reloj.Ahora.AddMinutes(_opciones.CodigoExpiraMinutos);
            await uow.GuardarAsync(ct);
            await correo.EnviarCodigoVerificacionAsync(usuario.Email, codigo, ct);
        }
        return new MensajeDto("Si la cuenta existe y está pendiente, se envió un nuevo código.");
    }

    public async Task<SesionDto> LoginAsync(LoginRequest r, CancellationToken ct)
    {
        var usuario = await usuarios.BuscarPorEmailAsync(Normalizar(r.Email), ct);
        if (usuario is null || !usuario.Activo) throw new CredencialesInvalidasException();
        if (!seguridad.VerificarPassword(r.Password, usuario.PasswordHash)) throw new CredencialesInvalidasException();
        if (!usuario.EmailVerificado) throw new CorreoNoVerificadoException();
        return await EmitirSesionAsync(usuario, ct);
    }

    public async Task<SesionDto> RefrescarAsync(RefrescarRequest r, CancellationToken ct)
    {
        var registro = await refreshTokens.BuscarPorHashAsync(seguridad.Sha256(r.RefreshToken), ct);
        if (registro is null || registro.Revocado || registro.Expira < reloj.Ahora)
            throw new NoAutenticadoException("Refresh token inválido o vencido");

        registro.Revocado = true;
        var usuario = await usuarios.BuscarPorIdAsync(registro.UsuarioId, ct);
        if (usuario is null || !usuario.Activo) throw new NoAutenticadoException("Usuario inactivo");
        return await EmitirSesionAsync(usuario, ct);
    }

    public async Task<MensajeDto> CerrarSesionAsync(RefrescarRequest r, CancellationToken ct)
    {
        var registro = await refreshTokens.BuscarPorHashAsync(seguridad.Sha256(r.RefreshToken), ct);
        if (registro is not null)
        {
            registro.Revocado = true;
            await uow.GuardarAsync(ct);
        }
        return new MensajeDto("Sesión cerrada");
    }

    public async Task<MensajeDto> OlvidePasswordAsync(EmailRequest r, CancellationToken ct)
    {
        var usuario = await usuarios.BuscarPorEmailAsync(Normalizar(r.Email), ct);
        if (usuario is not null && usuario.Activo && usuario.EmailVerificado)
        {
            var clave = seguridad.NuevaClaveTemporal();
            usuario.ClaveTemporalHash = seguridad.HashPassword(clave);
            usuario.ClaveTemporalExpira = reloj.Ahora.AddMinutes(_opciones.CodigoExpiraMinutos);
            await uow.GuardarAsync(ct);
            await correo.EnviarClaveTemporalAsync(usuario.Email, clave, ct);
        }
        return new MensajeDto("Si el correo está registrado, recibirás una clave temporal.");
    }

    public async Task<MensajeDto> RestablecerPasswordAsync(RestablecerRequest r, CancellationToken ct)
    {
        var usuario = await usuarios.BuscarPorEmailAsync(Normalizar(r.Email), ct);
        var valida = usuario is not null
            && usuario.ClaveTemporalHash is not null
            && usuario.ClaveTemporalExpira > reloj.Ahora
            && seguridad.VerificarPassword(r.ClaveTemporal, usuario.ClaveTemporalHash);
        if (!valida || usuario is null) throw new ValidacionException("La clave temporal es inválida o expiró");

        usuario.PasswordHash = seguridad.HashPassword(r.NuevaPassword);
        usuario.ClaveTemporalHash = null;
        usuario.ClaveTemporalExpira = null;
        await uow.GuardarAsync(ct);
        await refreshTokens.RevocarTodosAsync(usuario.Id, ct);
        return new MensajeDto("Contraseña actualizada. Inicia sesión con tu nueva contraseña.");
    }

    public async Task<UsuarioDto> PerfilAsync(int usuarioId, CancellationToken ct)
    {
        var usuario = await usuarios.BuscarPorIdAsync(usuarioId, ct)
            ?? throw new NoEncontradoException("Usuario no encontrado");
        return mapeador.Usuario(usuario);
    }

    private async Task<SesionDto> EmitirSesionAsync(Usuario usuario, CancellationToken ct)
    {
        var refresh = seguridad.NuevoRefreshToken();
        refreshTokens.Agregar(new RefreshToken
        {
            UsuarioId = usuario.Id,
            TokenHash = seguridad.Sha256(refresh),
            Expira = reloj.Ahora.AddDays(_opciones.RefreshTokenDias),
        });
        await uow.GuardarAsync(ct);
        return new SesionDto(seguridad.CrearAccessToken(usuario), refresh, seguridad.AccessTokenSegundos, mapeador.Usuario(usuario));
    }

    private static string Normalizar(string email) => email.Trim().ToLowerInvariant();
}
