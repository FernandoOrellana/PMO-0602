using Moq;
using PetCare.Application.Abstracciones;
using PetCare.Application.Servicios;
using PetCare.Domain;

namespace PetCare.Tests.Unitarias;

public class NotificacionesServiceTests
{
    private readonly Mock<INotificacionRepositorio> _notificaciones = new();
    private readonly Mock<IPushSender> _push = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private NotificacionesService Crear() => new(_notificaciones.Object, _push.Object, _uow.Object,
        Mocks.Reloj(new DateTime(2030, 1, 1, 8, 0, 0)).Object, Mocks.Mapeador());

    private void ConfigurarPush(int enviados, params string[] invalidos) =>
        _push.Setup(p => p.EnviarAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResultadoPush(enviados, invalidos));

    [Fact]
    public async Task Probar_GuardaLaNotificacionYEnviaPushConTipoPrueba()
    {
        _notificaciones.Setup(n => n.TokensDeUsuarioAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(["fcm-1", "fcm-2"]);
        ConfigurarPush(2);

        var resultado = await Crear().ProbarAsync(5, default);

        Assert.Equal(2, resultado.Dispositivos);
        Assert.Equal(2, resultado.Enviados);
        Assert.Equal("Notificación enviada a tus dispositivos.", resultado.Message);
        _notificaciones.Verify(n => n.Agregar(It.Is<Notificacion>(x => x.UsuarioId == 5)), Times.Once);
        _push.Verify(p => p.EnviarAsync(It.Is<IReadOnlyList<string>>(t => t.Count == 2), It.IsAny<string>(), It.IsAny<string>(),
            It.Is<IReadOnlyDictionary<string, string>>(d => d["tipo"] == "PRUEBA"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Probar_EliminaLosTokensInvalidos()
    {
        _notificaciones.Setup(n => n.TokensDeUsuarioAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(["fcm-1", "fcm-viejo"]);
        ConfigurarPush(1, "fcm-viejo");

        var resultado = await Crear().ProbarAsync(5, default);

        Assert.Equal(1, resultado.Enviados);
        _notificaciones.Verify(n => n.EliminarDispositivosAsync(
            It.Is<IReadOnlyList<string>>(t => t.Single() == "fcm-viejo"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Probar_SinDispositivosSoloGuardaLaNotificacion()
    {
        _notificaciones.Setup(n => n.TokensDeUsuarioAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<string>());

        var resultado = await Crear().ProbarAsync(5, default);

        Assert.Equal(0, resultado.Dispositivos);
        Assert.Equal("Notificación guardada. No hay dispositivos registrados para recibir push.", resultado.Message);
        _notificaciones.Verify(n => n.Agregar(It.IsAny<Notificacion>()), Times.Once);
        _push.Verify(p => p.EnviarAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
