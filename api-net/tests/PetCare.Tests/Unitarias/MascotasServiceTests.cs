using Moq;
using PetCare.Application.Abstracciones;
using PetCare.Application.Common;
using PetCare.Application.Contratos;
using PetCare.Application.Servicios;
using PetCare.Domain;

namespace PetCare.Tests.Unitarias;

public class MascotasServiceTests
{
    private readonly Mock<IMascotaRepositorio> _mascotas = new();
    private readonly Mock<IVacunacionRepositorio> _vacunas = new();
    private readonly Mock<ICitaRepositorio> _citas = new();
    private readonly Mock<ICatalogoRepositorio> _catalogo = new();
    private readonly Mock<IAlmacenArchivos> _almacen = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private MascotasService Crear() => new(
        _mascotas.Object,
        _vacunas.Object,
        _citas.Object,
        _catalogo.Object,
        _almacen.Object,
        _uow.Object,
        Mocks.Mapeador());

    [Fact]
    public async Task Historial_VeterinarioPuedeConsultarHistorialDeCualquierMascota()
    {
        var mascota = Datos.Mascota(1, duenoId: 10);
        _mascotas.Setup(m => m.PorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(mascota);
        _citas.Setup(c => c.ConsultasDeMascotaAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _vacunas.Setup(v => v.DeMascotaAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _citas.Setup(c => c.DeMascotaAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var actorVet = new Actor(99, Roles.Veterinario);
        var historial = await Crear().HistorialAsync(actorVet, 1, default);

        Assert.NotNull(historial);
        Assert.Equal(1, historial.Mascota.Id);
    }

    [Fact]
    public async Task Historial_AdministradorPuedeConsultarHistorialDeCualquierMascota()
    {
        var mascota = Datos.Mascota(1, duenoId: 10);
        _mascotas.Setup(m => m.PorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(mascota);
        _citas.Setup(c => c.ConsultasDeMascotaAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _vacunas.Setup(v => v.DeMascotaAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _citas.Setup(c => c.DeMascotaAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var actorAdmin = new Actor(1, Roles.Administrador);
        var historial = await Crear().HistorialAsync(actorAdmin, 1, default);

        Assert.NotNull(historial);
        Assert.Equal("Firulais", historial.Mascota.Nombre);
    }

    [Fact]
    public async Task Historial_DuenoNoPuedeConsultarMascotaAjena()
    {
        var mascota = Datos.Mascota(1, duenoId: 10);
        _mascotas.Setup(m => m.PorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(mascota);

        var actorDuenoAjeno = new Actor(20, Roles.Dueno);
        await Assert.ThrowsAsync<ProhibidoException>(() => Crear().HistorialAsync(actorDuenoAjeno, 1, default));
    }

    [Fact]
    public async Task CitasDeMascota_VinculaConsultaClinicaACitaAtendida()
    {
        var mascota = Datos.Mascota(1, duenoId: 5);
        var servicio = Datos.Servicio(1);
        var vet = Datos.Usuario(3, Roles.Veterinario);

        var cita1 = new Cita
        {
            Id = 100,
            MascotaId = 1,
            Mascota = mascota,
            ServicioId = 1,
            Servicio = servicio,
            VeterinarioId = 3,
            Veterinario = vet,
            Fecha = new DateOnly(2026, 11, 1),
            Hora = new TimeOnly(10, 0),
            Estado = EstadoCita.ATENDIDA,
        };

        var cita2 = new Cita
        {
            Id = 101,
            MascotaId = 1,
            Mascota = mascota,
            ServicioId = 1,
            Servicio = servicio,
            VeterinarioId = 3,
            Veterinario = vet,
            Fecha = new DateOnly(2026, 11, 15),
            Hora = new TimeOnly(11, 0),
            Estado = EstadoCita.CONFIRMADA,
        };

        var consulta = new Consulta
        {
            Id = 50,
            CitaId = 100,
            MascotaId = 1,
            VeterinarioId = 3,
            Veterinario = vet,
            Fecha = new DateOnly(2026, 11, 1),
            Diagnostico = "Infección de oído tratada con éxito",
            Tratamiento = "Gotas óticas cada 12h",
            PesoKg = 8.5m,
        };

        _mascotas.Setup(m => m.PorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(mascota);
        _citas.Setup(c => c.DeMascotaAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([cita1, cita2]);
        _citas.Setup(c => c.ConsultasDeMascotaAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([consulta]);

        var actorVet = new Actor(4, Roles.Veterinario); // Otro veterinario distinto al que atendió
        var citasDto = await Crear().CitasDeMascotaAsync(actorVet, 1, default);

        Assert.Equal(2, citasDto.Count);

        var dtoCita1 = citasDto.First(c => c.Id == 100);
        Assert.NotNull(dtoCita1.Consulta);
        Assert.Equal("Infección de oído tratada con éxito", dtoCita1.Consulta.Diagnostico);
        Assert.Equal("Gotas óticas cada 12h", dtoCita1.Consulta.Tratamiento);

        var dtoCita2 = citasDto.First(c => c.Id == 101);
        Assert.Null(dtoCita2.Consulta);
        Assert.Equal("CONFIRMADA", dtoCita2.Estado);
    }
}
