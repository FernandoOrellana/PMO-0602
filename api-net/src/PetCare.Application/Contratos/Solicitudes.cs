namespace PetCare.Application.Contratos;

public record RegistroRequest(string Nombre, string Email, string Password, string? Telefono);

public record VerificarEmailRequest(string Email, string Codigo);

public record EmailRequest(string Email);

public record LoginRequest(string Email, string Password);

public record RefrescarRequest(string RefreshToken);

public record RestablecerRequest(string Email, string ClaveTemporal, string NuevaPassword);

public record DispositivoRequest(string FcmToken);

public record MascotaRequest(string Nombre, int EspecieId, string? Raza, DateOnly? FechaNacimiento, decimal? PesoKg);

public record AplicacionVacunaRequest(int VacunaId, DateOnly FechaAplicacion, string? Lote, DateOnly? ProximaDosis, int? CitaId);

public record CitaRequest(int MascotaId, int ServicioId, int VeterinarioId, DateOnly Fecha, string Hora);

public record ConsultaRequest(string Diagnostico, string? Tratamiento, string? Observaciones, decimal? Peso);

public record ItemPedidoRequest(int ProductoId, int Cantidad);

public record PedidoRequest(IReadOnlyList<ItemPedidoRequest> Items);

public record EstadoPedidoRequest(PetCare.Domain.EstadoPedido Estado);

public record ProductoRequest(string Nombre, string? Descripcion, string Categoria, decimal Precio, int Stock);

public record ServicioRequest(string Nombre, decimal Precio, int DuracionMin);

public record VeterinarioCrearRequest(string Nombre, string Email, string Password, string? Telefono);

public record VeterinarioActualizarRequest(string Nombre, string? Telefono);

public record HorarioRequest(int DiaSemana, string HoraInicio, string HoraFin);
