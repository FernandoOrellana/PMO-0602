using Moq;
using PetCare.Application.Abstracciones;
using PetCare.Application.Common;
using PetCare.Application.Contratos;
using PetCare.Application.Servicios;
using PetCare.Domain;

namespace PetCare.Tests.Unitarias;

public class AuthServiceTests
{
    private readonly Mock<IUsuarioRepositorio> _usuarios = new();
    private readonly Mock<IRefreshTokenRepositorio> _tokens = new();
    private readonly Mock<INotificadorCorreo> _correo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ISeguridad> _seguridad = Mocks.Seguridad();
    private readonly DateTime _ahora = new(2030, 1, 1, 10, 0, 0);

    private AuthService Crear() => new(
        _usuarios.Object, _tokens.Object, _seguridad.Object, _correo.Object,
        Mocks.Reloj(_ahora).Object, _uow.Object, Mocks.Opciones(), Mocks.Mapeador());

    [Fact]
    public async Task Registrar_RechazaUnCorreoYaRegistrado()
    {
        _usuarios.Setup(u => u.BuscarPorEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Datos.Usuario(1, Roles.Dueno));

        await Assert.ThrowsAsync<ConflictoException>(() =>
            Crear().RegistrarAsync(new RegistroRequest("Ana", "ana@test.hn", "Clave12345", null), default));
        _usuarios.Verify(u => u.Agregar(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task Registrar_CreaDuenoNoVerificadoYEnviaCodigo()
    {
        _usuarios.Setup(u => u.BuscarPorEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Usuario?)null);
        _usuarios.Setup(u => u.IdRolAsync(Roles.Dueno, It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await Crear().RegistrarAsync(new RegistroRequest("Ana", "ANA@test.hn", "Clave12345", null), default);

        _usuarios.Verify(u => u.Agregar(It.Is<Usuario>(x => !x.EmailVerificado && x.Email == "ana@test.hn")), Times.Once);
        _correo.Verify(c => c.EnviarCodigoVerificacionAsync("ana@test.hn", "123456", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_RechazaContrasenaIncorrecta()
    {
        var usuario = Datos.Usuario(1, Roles.Dueno);
        _usuarios.Setup(u => u.BuscarPorEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

        await Assert.ThrowsAsync<CredencialesInvalidasException>(() =>
            Crear().LoginAsync(new LoginRequest("u1@test.hn", "otra"), default));
    }

    [Fact]
    public async Task Login_ExigeCorreoVerificado()
    {
        var usuario = Datos.Usuario(1, Roles.Dueno, verificado: false);
        usuario.PasswordHash = "hash:Clave12345";
        _usuarios.Setup(u => u.BuscarPorEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

        await Assert.ThrowsAsync<CorreoNoVerificadoException>(() =>
            Crear().LoginAsync(new LoginRequest("u1@test.hn", "Clave12345"), default));
    }

    [Fact]
    public async Task Login_EmiteAccessTokenYGuardaElRefreshComoHash()
    {
        var usuario = Datos.Usuario(5, Roles.Dueno);
        usuario.PasswordHash = "hash:Clave12345";
        _usuarios.Setup(u => u.BuscarPorEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

        var sesion = await Crear().LoginAsync(new LoginRequest("u5@test.hn", "Clave12345"), default);

        Assert.Equal("access-token", sesion.AccessToken);
        Assert.Equal("refresh-token", sesion.RefreshToken);
        Assert.Equal(1800, sesion.ExpiresIn);
        _tokens.Verify(t => t.Agregar(It.Is<RefreshToken>(r => r.UsuarioId == 5 && r.TokenHash == "sha:refresh-token")), Times.Once);
    }

    [Fact]
    public async Task VerificarEmail_RechazaUnCodigoIncorrecto()
    {
        var usuario = Datos.Usuario(1, Roles.Dueno, verificado: false);
        usuario.CodigoVerificacionHash = "sha:123456";
        usuario.CodigoExpira = _ahora.AddMinutes(10);
        _usuarios.Setup(u => u.BuscarPorEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

        await Assert.ThrowsAsync<ValidacionException>(() =>
            Crear().VerificarEmailAsync(new VerificarEmailRequest("u1@test.hn", "000000"), default));
        Assert.False(usuario.EmailVerificado);
    }

    [Fact]
    public async Task RestablecerPassword_RechazaUnaClaveTemporalVencida()
    {
        var usuario = Datos.Usuario(1, Roles.Dueno);
        usuario.ClaveTemporalHash = "hash:Temp1234";
        usuario.ClaveTemporalExpira = _ahora.AddMinutes(-1);
        _usuarios.Setup(u => u.BuscarPorEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

        await Assert.ThrowsAsync<ValidacionException>(() =>
            Crear().RestablecerPasswordAsync(new RestablecerRequest("u1@test.hn", "Temp1234", "NuevaClave1"), default));
    }

    [Fact]
    public async Task RestablecerPassword_ActualizaLaClaveYRevocaLasSesiones()
    {
        var usuario = Datos.Usuario(1, Roles.Dueno);
        usuario.ClaveTemporalHash = "hash:Temp1234";
        usuario.ClaveTemporalExpira = _ahora.AddMinutes(10);
        _usuarios.Setup(u => u.BuscarPorEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

        await Crear().RestablecerPasswordAsync(new RestablecerRequest("u1@test.hn", "Temp1234", "NuevaClave1"), default);

        Assert.Equal("hash:NuevaClave1", usuario.PasswordHash);
        Assert.Null(usuario.ClaveTemporalHash);
        _tokens.Verify(t => t.RevocarTodosAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }
}
