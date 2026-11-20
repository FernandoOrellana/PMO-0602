using PetCare.Application.Abstracciones;
using PetCare.Application.Common;
using PetCare.Application.Contratos;
using PetCare.Domain;

namespace PetCare.Application.Servicios;

public class MascotasService(
    IMascotaRepositorio mascotas,
    IVacunacionRepositorio vacunacion,
    ICitaRepositorio citas,
    ICatalogoRepositorio catalogo,
    IAlmacenArchivos almacen,
    IUnitOfWork uow,
    Mapeador mapeador)
{
    public async Task<Pagina<MascotaDto>> ListarAsync(Actor actor, int page, int limit, CancellationToken ct)
    {
        var (items, total) = await mascotas.PaginarAsync(actor.Rol == Roles.Dueno ? actor.Id : null, page, limit, ct);
        return new Pagina<MascotaDto>(items.Select(mapeador.Mascota).ToList(), Paginacion.Crear(page, limit, total));
    }

    public async Task<MascotaDto> CrearAsync(Actor actor, MascotaRequest r, CancellationToken ct)
    {
        await AsegurarEspecie(r.EspecieId, ct);
        var mascota = new Mascota
        {
            DuenoId = actor.Id,
            EspecieId = r.EspecieId,
            Nombre = r.Nombre.Trim(),
            Raza = r.Raza,
            FechaNacimiento = r.FechaNacimiento,
            PesoKg = r.PesoKg,
        };
        mascotas.Agregar(mascota);
        await uow.GuardarAsync(ct);
        return mapeador.Mascota(await Cargar(mascota.Id, ct));
    }

    public async Task<MascotaDto> ObtenerAsync(Actor actor, int id, CancellationToken ct) =>
        mapeador.Mascota(await Accesible(actor, id, ct));

    public async Task<MascotaDto> ActualizarAsync(Actor actor, int id, MascotaRequest r, CancellationToken ct)
    {
        var mascota = await Propietario(actor, id, ct);
        await AsegurarEspecie(r.EspecieId, ct);
        mascota.Nombre = r.Nombre.Trim();
        mascota.EspecieId = r.EspecieId;
        mascota.Raza = r.Raza;
        mascota.FechaNacimiento = r.FechaNacimiento;
        mascota.PesoKg = r.PesoKg;
        await uow.GuardarAsync(ct);
        return mapeador.Mascota(await Cargar(id, ct));
    }

    public async Task<MensajeDto> EliminarAsync(Actor actor, int id, CancellationToken ct)
    {
        var mascota = await Propietario(actor, id, ct);
        mascota.Activo = false;
        await uow.GuardarAsync(ct);
        return new MensajeDto("Mascota eliminada");
    }

    public async Task<MascotaDto> ActualizarFotoAsync(Actor actor, int id, Stream contenido, string extension, CancellationToken ct)
    {
        var mascota = await Propietario(actor, id, ct);
        var anterior = mascota.FotoRuta;
        mascota.FotoRuta = await almacen.GuardarAsync("mascotas", contenido, extension, ct);
        await uow.GuardarAsync(ct);
        await almacen.EliminarAsync(anterior, ct);
        return mapeador.Mascota(await Cargar(id, ct));
    }

    public async Task<CarneDto> CarneAsync(Actor actor, int id, CancellationToken ct)
    {
        var mascota = await Accesible(actor, id, ct);
        var vacunas = await vacunacion.DeMascotaAsync(id, ct);
        var proximas = Reglas.ProximasDosis(vacunas,
            a => a.VacunaId, a => a.FechaAplicacion, a => a.ProximaDosis);
        return new CarneDto(
            mapeador.Mascota(mascota),
            vacunas.Select(mapeador.Aplicacion).ToList(),
            proximas.Select(a => new ProximaDosisDto(new ResumenDto(a.Vacuna.Id, a.Vacuna.Nombre), a.ProximaDosis!.Value)).ToList());
    }

    public async Task<HistorialDto> HistorialAsync(Actor actor, int id, CancellationToken ct)
    {
        var mascota = await Accesible(actor, id, ct);
        var consultas = await citas.ConsultasDeMascotaAsync(id, ct);
        var vacunas = await vacunacion.DeMascotaAsync(id, ct);
        var citasMascota = await citas.DeMascotaAsync(id, ct);
        var consultasDto = consultas.Select(mapeador.Consulta).ToList();
        var consultasPorCita = consultasDto.ToDictionary(c => c.CitaId);
        var citasDto = citasMascota.Select(c => mapeador.Cita(c, consultasPorCita.GetValueOrDefault(c.Id))).ToList();

        return new HistorialDto(
            mapeador.Mascota(mascota),
            consultasDto,
            vacunas.Select(mapeador.Aplicacion).ToList(),
            citasDto);
    }

    public async Task<IReadOnlyList<CitaDto>> CitasDeMascotaAsync(Actor actor, int id, CancellationToken ct)
    {
        await Accesible(actor, id, ct);
        var consultas = await citas.ConsultasDeMascotaAsync(id, ct);
        var citasMascota = await citas.DeMascotaAsync(id, ct);
        var consultasDto = consultas.Select(mapeador.Consulta).ToList();
        var consultasPorCita = consultasDto.ToDictionary(c => c.CitaId);
        return citasMascota.Select(c => mapeador.Cita(c, consultasPorCita.GetValueOrDefault(c.Id))).ToList();
    }

    public async Task<AplicacionVacunaDto> RegistrarVacunaAsync(Actor actor, int mascotaId, AplicacionVacunaRequest r, CancellationToken ct)
    {
        await Accesible(actor, mascotaId, ct);
        var vacuna = await catalogo.VacunaPorIdAsync(r.VacunaId, ct)
            ?? throw new ValidacionException("La vacuna seleccionada no existe");

        var proxima = r.ProximaDosis ?? Reglas.ProximaDosis(r.FechaAplicacion, vacuna.IntervaloDias);
        if (proxima < r.FechaAplicacion)
            throw new ValidacionException("La próxima dosis no puede ser anterior a la aplicación");

        var aplicacion = new AplicacionVacuna
        {
            MascotaId = mascotaId,
            VacunaId = r.VacunaId,
            VeterinarioId = actor.Id,
            CitaId = r.CitaId,
            FechaAplicacion = r.FechaAplicacion,
            ProximaDosis = proxima,
            Lote = r.Lote,
        };
        vacunacion.Agregar(aplicacion);
        await uow.GuardarAsync(ct);
        return mapeador.Aplicacion(await vacunacion.PorIdAsync(aplicacion.Id, ct) ?? throw new NoEncontradoException());
    }

    private async Task AsegurarEspecie(int especieId, CancellationToken ct)
    {
        if (!await catalogo.EspecieExisteAsync(especieId, ct))
            throw new ValidacionException("La especie seleccionada no existe");
    }

    private async Task<Mascota> Cargar(int id, CancellationToken ct) =>
        await mascotas.PorIdAsync(id, ct) ?? throw new NoEncontradoException("Mascota no encontrada");

    private async Task<Mascota> Accesible(Actor actor, int id, CancellationToken ct)
    {
        var mascota = await mascotas.PorIdAsync(id, ct);
        if (mascota is null || !mascota.Activo) throw new NoEncontradoException("Mascota no encontrada");
        if (actor.Rol == Roles.Dueno && mascota.DuenoId != actor.Id)
            throw new ProhibidoException("Esta mascota no pertenece a tu cuenta");
        return mascota;
    }

    private async Task<Mascota> Propietario(Actor actor, int id, CancellationToken ct)
    {
        var mascota = await Accesible(actor, id, ct);
        if (mascota.DuenoId != actor.Id) throw new ProhibidoException("Esta mascota no pertenece a tu cuenta");
        return mascota;
    }
}
