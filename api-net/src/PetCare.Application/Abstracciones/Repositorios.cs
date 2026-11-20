using PetCare.Application.Contratos;
using PetCare.Domain;

namespace PetCare.Application.Abstracciones;

public interface IUsuarioRepositorio
{
    Task<Usuario?> BuscarPorEmailAsync(string email, CancellationToken ct);
    Task<Usuario?> BuscarPorIdAsync(int id, CancellationToken ct);
    Task<int> IdRolAsync(string nombre, CancellationToken ct);
    void Agregar(Usuario usuario);
}

public interface IRefreshTokenRepositorio
{
    void Agregar(RefreshToken token);
    Task<RefreshToken?> BuscarPorHashAsync(string hash, CancellationToken ct);
    Task RevocarTodosAsync(int usuarioId, CancellationToken ct);
}

public interface ICatalogoRepositorio
{
    Task<IReadOnlyList<Especie>> EspeciesAsync(CancellationToken ct);
    Task<bool> EspecieExisteAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<Vacuna>> VacunasAsync(CancellationToken ct);
    Task<Vacuna?> VacunaPorIdAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<Servicio>> ServiciosAsync(bool soloActivos, CancellationToken ct);
    Task<Servicio?> ServicioPorIdAsync(int id, CancellationToken ct);
    void AgregarServicio(Servicio servicio);
}

public interface IVeterinarioRepositorio
{
    Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken ct);
    Task<Usuario?> PorIdAsync(int id, CancellationToken ct);
}

public interface IMascotaRepositorio
{
    Task<(IReadOnlyList<Mascota> Items, int Total)> PaginarAsync(int? duenoId, int page, int limit, CancellationToken ct);
    Task<Mascota?> PorIdAsync(int id, CancellationToken ct);
    void Agregar(Mascota mascota);
}

public interface IVacunacionRepositorio
{
    Task<IReadOnlyList<AplicacionVacuna>> DeMascotaAsync(int mascotaId, CancellationToken ct);
    Task<AplicacionVacuna?> PorIdAsync(int id, CancellationToken ct);
    void Agregar(AplicacionVacuna aplicacion);
    Task<IReadOnlyList<AplicacionVacuna>> PendientesRecordatorioAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);
}

public interface ICitaRepositorio
{
    Task<Cita?> PorIdAsync(int id, CancellationToken ct);
    Task<(IReadOnlyList<Cita> Items, int Total)> PaginarAsync(FiltroCitas filtro, CancellationToken ct);
    Task<IReadOnlyList<Cita>> DeMascotaAsync(int mascotaId, CancellationToken ct);
    Task<IReadOnlyList<Domain.Ocupacion>> ActivasPorVeterinarioYFechaAsync(int veterinarioId, DateOnly fecha, CancellationToken ct);
    Task<IReadOnlyList<Consulta>> ConsultasDeMascotaAsync(int mascotaId, CancellationToken ct);
    Task<Consulta?> ConsultaPorCitaAsync(int citaId, CancellationToken ct);
    Task<IReadOnlyList<Cita>> PendientesRecordatorioAsync(DateOnly fecha, CancellationToken ct);
    void Agregar(Cita cita);
    void AgregarConsulta(Consulta consulta);
}

public interface IProductoRepositorio
{
    Task<(IReadOnlyList<Producto> Items, int Total)> PaginarAsync(string? categoria, bool soloActivos, int page, int limit, CancellationToken ct);
    Task<Producto?> PorIdAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<Producto>> PorIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct);
    void Agregar(Producto producto);
}

public interface IPedidoRepositorio
{
    Task<(IReadOnlyList<Pedido> Items, int Total)> PaginarAsync(int? clienteId, int page, int limit, CancellationToken ct);
    Task<Pedido?> PorIdAsync(int id, CancellationToken ct);
    void Agregar(Pedido pedido);
}

public interface INotificacionRepositorio
{
    Task<IReadOnlyList<Notificacion>> DeUsuarioAsync(int usuarioId, CancellationToken ct);
    Task<Notificacion?> DeUsuarioPorIdAsync(int id, int usuarioId, CancellationToken ct);
    void Agregar(Notificacion notificacion);
    Task<IReadOnlyList<string>> TokensDeUsuarioAsync(int usuarioId, CancellationToken ct);
    Task GuardarDispositivoAsync(int usuarioId, string fcmToken, CancellationToken ct);
    Task EliminarDispositivosAsync(IReadOnlyList<string> fcmTokens, CancellationToken ct);
}

public interface IReporteRepositorio
{
    Task<VentasResumen> VentasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);
    Task<CitasResumen> CitasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);
}

public record FiltroCitas(int? DuenoId, int? VeterinarioId, int? MascotaId, DateOnly? Fecha, Domain.EstadoCita? Estado, int Page, int Limit);
