using Moq;
using PetCare.Application.Abstracciones;
using PetCare.Application.Common;
using PetCare.Application.Contratos;
using PetCare.Application.Servicios;
using PetCare.Domain;

namespace PetCare.Tests.Unitarias;

public class CitasServiceTests
{
    private readonly Mock<ICitaRepositorio> _citas = new();
    private readonly Mock<IMascotaRepositorio> _mascotas = new();
    private readonly Mock<ICatalogoRepositorio> _catalogo = new();
    private readonly Mock<IVeterinarioRepositorio> _veterinarios = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly DateTime _ahora = new(2030, 1, 1, 8, 0, 0);

    private CitasService Crear() => new(_citas.Object, _mascotas.Object, _catalogo.Object,
        _veterinarios.Object, _uow.Object, Mocks.Reloj(_ahora).Object, Mocks.Mapeador());

    private static CitaRequest Solicitud(string hora = "09:00") =>
        new(1, 1, 3, new DateOnly(2030, 1, 1), hora);

    private void ConfigurarVeterinario()
    {
        var vet = Datos.Usuario(3, Roles.Veterinario);
        vet.Horarios = [new VeterinarioHorario { VeterinarioId = 3, DiaSemana = 2, HoraInicio = new TimeOnly(8, 0), HoraFin = new TimeOnly(12, 0) }];
        _veterinarios.Setup(v => v.PorIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(vet);
        _catalogo.Setup(c => c.ServicioPorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Datos.Servicio());
    }

    [Fact]
    public async Task Agendar_RechazaUnaMascotaQueNoEsDelDueno()
    {
        _mascotas.Setup(m => m.PorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Datos.Mascota(1, duenoId: 5));

        await Assert.ThrowsAsync<ProhibidoException>(() => Crear().AgendarAsync(new Actor(9, Roles.Dueno), Solicitud(), default));
        _citas.Verify(c => c.Agregar(It.IsAny<Cita>()), Times.Never);
    }

    [Fact]
    public async Task Agendar_RechazaUnaHoraYaOcupada()
    {
        _mascotas.Setup(m => m.PorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Datos.Mascota(1, duenoId: 5));
        ConfigurarVeterinario();
        _citas.Setup(c => c.ActivasPorVeterinarioYFechaAsync(3, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Ocupacion(new TimeOnly(9, 0), 30)]);

        await Assert.ThrowsAsync<ConflictoException>(() => Crear().AgendarAsync(new Actor(5, Roles.Dueno), Solicitud(), default));
        _citas.Verify(c => c.Agregar(It.IsAny<Cita>()), Times.Never);
    }

    [Fact]
    public async Task Cancelar_RechazaCancelarLaCitaDeOtroDueno()
    {
        var cita = new Cita { Id = 7, Mascota = Datos.Mascota(1, duenoId: 5), Servicio = Datos.Servicio(), Veterinario = Datos.Usuario(3, Roles.Veterinario), Estado = EstadoCita.CONFIRMADA };
        _citas.Setup(c => c.PorIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(cita);

        await Assert.ThrowsAsync<ProhibidoException>(() => Crear().CancelarAsync(new Actor(8, Roles.Dueno), 7, default));
        Assert.Equal(EstadoCita.CONFIRMADA, cita.Estado);
    }

    [Fact]
    public async Task RegistrarConsulta_SoloLoPuedeHacerElVeterinarioAsignado()
    {
        var cita = new Cita { Id = 7, MascotaId = 1, Fecha = new DateOnly(2030, 1, 1), Mascota = Datos.Mascota(1, 5), Servicio = Datos.Servicio(), Veterinario = Datos.Usuario(3, Roles.Veterinario), VeterinarioId = 3, Estado = EstadoCita.CONFIRMADA };
        _citas.Setup(c => c.PorIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(cita);

        await Assert.ThrowsAsync<ProhibidoException>(() =>
            Crear().RegistrarConsultaAsync(new Actor(4, Roles.Veterinario), 7, new ConsultaRequest("Sano", null, null, null), default));
        _citas.Verify(c => c.AgregarConsulta(It.IsAny<Consulta>()), Times.Never);
    }

    [Fact]
    public async Task ActualizarObservaciones_ActualizaConsultaExistente()
    {
        var cita = new Cita { Id = 7, MascotaId = 1, Fecha = new DateOnly(2030, 1, 1), Mascota = Datos.Mascota(1, 5), Servicio = Datos.Servicio(), Veterinario = Datos.Usuario(3, Roles.Veterinario), VeterinarioId = 3, Estado = EstadoCita.ATENDIDA };
        var consulta = new Consulta { Id = 10, CitaId = 7, MascotaId = 1, VeterinarioId = 3, Diagnostico = "Gripe", Observaciones = "Previo", Veterinario = cita.Veterinario };
        
        _citas.Setup(c => c.PorIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(cita);
        _citas.Setup(c => c.ConsultaPorCitaAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(consulta);

        var res = await Crear().ActualizarObservacionesAsync(new Actor(3, Roles.Veterinario), 7, "Nueva observacion clinica", default);

        Assert.Equal("Nueva observacion clinica", res.Observaciones);
        Assert.Equal("Nueva observacion clinica", consulta.Observaciones);
        _uow.Verify(u => u.GuardarAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActualizarObservaciones_CreaConsultaSiNoExiste()
    {
        var cita = new Cita { Id = 8, MascotaId = 1, Fecha = new DateOnly(2030, 1, 1), Mascota = Datos.Mascota(1, 5), Servicio = Datos.Servicio(), Veterinario = Datos.Usuario(3, Roles.Veterinario), VeterinarioId = 3, Estado = EstadoCita.PENDIENTE };
        
        _citas.Setup(c => c.PorIdAsync(8, It.IsAny<CancellationToken>())).ReturnsAsync(cita);
        _citas.Setup(c => c.ConsultaPorCitaAsync(8, It.IsAny<CancellationToken>())).ReturnsAsync((Consulta?)null);
        _veterinarios.Setup(v => v.PorIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(cita.Veterinario);

        var res = await Crear().ActualizarObservacionesAsync(new Actor(3, Roles.Veterinario), 8, "Observacion inicial", default);

        Assert.Equal("Observacion inicial", res.Observaciones);
        Assert.Equal(EstadoCita.ATENDIDA, cita.Estado);
        _citas.Verify(c => c.AgregarConsulta(It.Is<Consulta>(con => con.Observaciones == "Observacion inicial" && con.CitaId == 8)), Times.Once);
        _uow.Verify(u => u.GuardarAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActualizarObservaciones_AdministradorNoQuedaComoVeterinarioDeLaConsulta()
    {
        var cita = new Cita { Id = 9, MascotaId = 1, Fecha = new DateOnly(2030, 1, 1), Mascota = Datos.Mascota(1, 5), Servicio = Datos.Servicio(), Veterinario = Datos.Usuario(3, Roles.Veterinario), VeterinarioId = 3, Estado = EstadoCita.CONFIRMADA };

        _citas.Setup(c => c.PorIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(cita);
        _citas.Setup(c => c.ConsultaPorCitaAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync((Consulta?)null);

        await Crear().ActualizarObservacionesAsync(new Actor(1, Roles.Administrador), 9, "Nota del admin", default);

        _citas.Verify(c => c.AgregarConsulta(It.Is<Consulta>(con => con.VeterinarioId == 3 && con.Observaciones == "Nota del admin")), Times.Once);
    }
}
