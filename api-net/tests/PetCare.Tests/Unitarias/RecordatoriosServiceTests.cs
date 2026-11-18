using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PetCare.Application.Abstracciones;
using PetCare.Application.Servicios;
using PetCare.Domain;

namespace PetCare.Tests.Unitarias;

public class RecordatoriosServiceTests
{
    private readonly Mock<IVacunacionRepositorio> _vacunacion = new();
    private readonly Mock<ICitaRepositorio> _citas = new();
    private readonly Mock<INotificacionRepositorio> _notificaciones = new();
    private readonly Mock<IPushSender> _push = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private RecordatoriosService Crear() => new(_vacunacion.Object, _citas.Object, _notificaciones.Object,
        _push.Object, _uow.Object, Mocks.Opciones(), Mocks.Reloj(new DateTime(2030, 1, 1, 8, 0, 0)).Object,
        NullLogger<RecordatoriosService>.Instance);

    private void ConfigurarDatos()
    {
        var mascota = Datos.Mascota(1, duenoId: 5);
        _vacunacion.Setup(v => v.PendientesRecordatorioAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new AplicacionVacuna { Id = 4, Mascota = mascota, MascotaId = 1, Vacuna = new Vacuna { Nombre = "Antirrábica" }, ProximaDosis = new DateOnly(2030, 1, 5) }]);
        _citas.Setup(c => c.PendientesRecordatorioAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Cita { Id = 9, Mascota = mascota, Servicio = Datos.Servicio(), Fecha = new DateOnly(2030, 1, 2), Hora = new TimeOnly(9, 0) }]);
        _notificaciones.Setup(n => n.TokensDeUsuarioAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(["fcm-1"]);
    }

    [Fact]
    public async Task Ejecutar_NotificaVacunasYCitasYMarcaCadaAviso()
    {
        ConfigurarDatos();
        var vacuna = (await _vacunacion.Object.PendientesRecordatorioAsync(default, default, default))[0];
        var cita = (await _citas.Object.PendientesRecordatorioAsync(default, default))[0];

        var resultado = await Crear().EjecutarAsync(default);

        Assert.Equal(1, resultado.Vacunas);
        Assert.Equal(1, resultado.Citas);
        Assert.True(vacuna.RecordatorioEnviado);
        Assert.True(cita.RecordatorioEnviado);
        _notificaciones.Verify(n => n.Agregar(It.IsAny<Notificacion>()), Times.Exactly(2));
        _push.Verify(p => p.EnviarAsync(It.Is<IReadOnlyList<string>>(t => t.Single() == "fcm-1"), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Ejecutar_UnFalloDePushNoDetieneLosDemasAvisos()
    {
        ConfigurarDatos();
        _push.Setup(p => p.EnviarAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("FCM caído"));

        var resultado = await Crear().EjecutarAsync(default);

        Assert.Equal(1, resultado.Citas);
        _notificaciones.Verify(n => n.Agregar(It.IsAny<Notificacion>()), Times.Exactly(2));
    }
}
