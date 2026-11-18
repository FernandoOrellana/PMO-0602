namespace PetCare.Application.Contratos;

public record MensajeDto(string Message);

public record ResumenDto(int Id, string Nombre);

public record UsuarioDto(int Id, string Nombre, string Email, string? Telefono, string Rol);

public record SesionDto(string AccessToken, string RefreshToken, int ExpiresIn, UsuarioDto Usuario);

public record EspecieDto(int Id, string Nombre);

public record VacunaDto(int Id, string Nombre, int IntervaloDias);

public record ServicioDto(int Id, string Nombre, decimal Precio, int DuracionMin, bool Activo);

public record ServicioResumenDto(int Id, string Nombre, decimal Precio);

public record HorarioDto(int DiaSemana, string HoraInicio, string HoraFin);

public record VeterinarioDto(int Id, string Nombre, string Email, string? Telefono, IReadOnlyList<HorarioDto> Horarios);

public record MascotaDto(
    int Id,
    int DuenoId,
    string Nombre,
    ResumenDto Especie,
    string? Raza,
    DateOnly? FechaNacimiento,
    decimal? PesoKg,
    string? FotoUrl,
    bool Activo);

public record AplicacionVacunaDto(
    int Id,
    ResumenDto Vacuna,
    DateOnly FechaAplicacion,
    DateOnly? ProximaDosis,
    string? Lote,
    ResumenDto Veterinario);

public record ProximaDosisDto(ResumenDto Vacuna, DateOnly Fecha);

public record CarneDto(MascotaDto Mascota, IReadOnlyList<AplicacionVacunaDto> Vacunas, IReadOnlyList<ProximaDosisDto> ProximasDosis);

public record ConsultaDto(
    int Id,
    int CitaId,
    DateOnly Fecha,
    string Diagnostico,
    string? Tratamiento,
    string? Observaciones,
    decimal? Peso,
    ResumenDto Veterinario);

public record DuenoDto(int Id, string Nombre, string? Telefono);

public record CitaDto(
    int Id,
    DateOnly Fecha,
    string Hora,
    string Estado,
    ResumenDto Mascota,
    ServicioResumenDto Servicio,
    ResumenDto Veterinario,
    DuenoDto Dueno);

public record HistorialDto(MascotaDto Mascota, IReadOnlyList<ConsultaDto> Consultas, IReadOnlyList<AplicacionVacunaDto> Vacunas, IReadOnlyList<CitaDto> Citas);

public record DisponibilidadDto(DateOnly Fecha, ResumenDto Veterinario, IReadOnlyList<string> Horas);

public record ProductoDto(int Id, string Nombre, string? Descripcion, string Categoria, decimal Precio, int Stock, string? ImagenUrl, bool Activo);

public record PedidoItemDto(int ProductoId, string Nombre, decimal PrecioUnitario, int Cantidad, decimal Subtotal);

public record PedidoDto(int Id, DateTime Fecha, string Estado, decimal Total, IReadOnlyList<PedidoItemDto> Items, ResumenDto Cliente);

public record NotificacionDto(int Id, string Titulo, string Mensaje, bool Leida, DateTime Fecha);

public record VentaProductoDto(int ProductoId, string Nombre, int Cantidad, decimal Total);

public record VentasResumen(int TotalPedidos, decimal TotalVentas, IReadOnlyList<VentaProductoDto> Productos);

public record ReporteVentasDto(DateOnly Desde, DateOnly Hasta, int TotalPedidos, decimal TotalVentas, IReadOnlyList<VentaProductoDto> Productos);

public record CitasServicioDto(int ServicioId, string Nombre, int Cantidad);

public record CitasResumen(int Total, IReadOnlyDictionary<string, int> PorEstado, IReadOnlyList<CitasServicioDto> PorServicio);

public record ReporteCitasDto(DateOnly Desde, DateOnly Hasta, int Total, IReadOnlyDictionary<string, int> PorEstado, IReadOnlyList<CitasServicioDto> PorServicio);
