using PetCare.Application.Abstracciones;
using PetCare.Application.Common;
using PetCare.Application.Contratos;
using PetCare.Domain;

namespace PetCare.Application.Servicios;

public class CatalogosService(
    ICatalogoRepositorio catalogo,
    IVeterinarioRepositorio veterinarios,
    ICitaRepositorio citas,
    IReloj reloj,
    Mapeador mapeador)
{
    public async Task<IReadOnlyList<EspecieDto>> EspeciesAsync(CancellationToken ct) =>
        (await catalogo.EspeciesAsync(ct)).Select(mapeador.Especie).ToList();

    public async Task<IReadOnlyList<VacunaDto>> VacunasAsync(CancellationToken ct) =>
        (await catalogo.VacunasAsync(ct)).Select(mapeador.Vacuna).ToList();

    public async Task<IReadOnlyList<ServicioDto>> ServiciosAsync(CancellationToken ct) =>
        (await catalogo.ServiciosAsync(true, ct)).Select(mapeador.Servicio).ToList();

    public async Task<IReadOnlyList<VeterinarioDto>> VeterinariosAsync(CancellationToken ct) =>
        (await veterinarios.ListarAsync(ct)).Select(mapeador.Veterinario).ToList();

    public async Task<DisponibilidadDto> DisponibilidadAsync(int veterinarioId, DateOnly fecha, int? servicioId, CancellationToken ct)
    {
        var veterinario = await veterinarios.PorIdAsync(veterinarioId, ct)
            ?? throw new NoEncontradoException("Veterinario no encontrado");

        var duracion = Parametros.SlotMinutos;
        if (servicioId is not null)
        {
            var servicio = await catalogo.ServicioPorIdAsync(servicioId.Value, ct);
            if (servicio is null || !servicio.Activo) throw new NoEncontradoException("Servicio no encontrado");
            duracion = servicio.DuracionMin;
        }

        var horas = fecha < DateOnly.FromDateTime(reloj.Ahora)
            ? []
            : Reglas.HorasDisponibles(
                fecha,
                veterinario.Horarios.Select(h => new Franja(h.DiaSemana, h.HoraInicio, h.HoraFin)),
                await citas.ActivasPorVeterinarioYFechaAsync(veterinarioId, fecha, ct),
                duracion,
                reloj.Ahora);

        return new DisponibilidadDto(
            fecha,
            new ResumenDto(veterinario.Id, veterinario.Nombre),
            horas.Select(h => h.ToString("HH:mm")).ToList());
    }
}
