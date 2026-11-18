namespace PetCare.Domain;

public class Rol
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
}

public class Usuario
{
    public int Id { get; set; }
    public int RolId { get; set; }
    public Rol Rol { get; set; } = null!;
    public string Nombre { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Telefono { get; set; }
    public string PasswordHash { get; set; } = "";
    public bool EmailVerificado { get; set; }
    public string? CodigoVerificacionHash { get; set; }
    public DateTime? CodigoExpira { get; set; }
    public string? ClaveTemporalHash { get; set; }
    public DateTime? ClaveTemporalExpira { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public List<VeterinarioHorario> Horarios { get; set; } = [];
}

public class RefreshToken
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTime Expira { get; set; }
    public bool Revocado { get; set; }
}

public class Dispositivo
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string FcmToken { get; set; } = "";
}

public class Especie
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
}

public class Vacuna
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public int IntervaloDias { get; set; }
}

public class Servicio
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int DuracionMin { get; set; } = 30;
    public bool Activo { get; set; } = true;
}

public class VeterinarioHorario
{
    public int Id { get; set; }
    public int VeterinarioId { get; set; }
    public int DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
}

public class Mascota
{
    public int Id { get; set; }
    public int DuenoId { get; set; }
    public Usuario Dueno { get; set; } = null!;
    public int EspecieId { get; set; }
    public Especie Especie { get; set; } = null!;
    public string Nombre { get; set; } = "";
    public string? Raza { get; set; }
    public DateOnly? FechaNacimiento { get; set; }
    public decimal? PesoKg { get; set; }
    public string? FotoRuta { get; set; }
    public bool Activo { get; set; } = true;
}

public class Cita
{
    public int Id { get; set; }
    public int MascotaId { get; set; }
    public Mascota Mascota { get; set; } = null!;
    public int ServicioId { get; set; }
    public Servicio Servicio { get; set; } = null!;
    public int VeterinarioId { get; set; }
    public Usuario Veterinario { get; set; } = null!;
    public DateOnly Fecha { get; set; }
    public TimeOnly Hora { get; set; }
    public EstadoCita Estado { get; set; } = EstadoCita.PENDIENTE;
    public bool RecordatorioEnviado { get; set; }
}

public class Consulta
{
    public int Id { get; set; }
    public int CitaId { get; set; }
    public int MascotaId { get; set; }
    public int VeterinarioId { get; set; }
    public Usuario Veterinario { get; set; } = null!;
    public DateOnly Fecha { get; set; }
    public string Diagnostico { get; set; } = "";
    public string? Tratamiento { get; set; }
    public string? Observaciones { get; set; }
    public decimal? PesoKg { get; set; }
}

public class AplicacionVacuna
{
    public int Id { get; set; }
    public int MascotaId { get; set; }
    public Mascota Mascota { get; set; } = null!;
    public int VacunaId { get; set; }
    public Vacuna Vacuna { get; set; } = null!;
    public int VeterinarioId { get; set; }
    public Usuario Veterinario { get; set; } = null!;
    public int? CitaId { get; set; }
    public DateOnly FechaAplicacion { get; set; }
    public DateOnly? ProximaDosis { get; set; }
    public string? Lote { get; set; }
    public bool RecordatorioEnviado { get; set; }
}

public class Producto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public string Categoria { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public string? ImagenRuta { get; set; }
    public bool Activo { get; set; } = true;
}

public class Pedido
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public Usuario Cliente { get; set; } = null!;
    public DateTime Fecha { get; set; }
    public EstadoPedido Estado { get; set; } = EstadoPedido.PENDIENTE;
    public decimal Total { get; set; }
    public List<PedidoItem> Items { get; set; } = [];
}

public class PedidoItem
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public Pedido Pedido { get; set; } = null!;
    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    public string NombreProducto { get; set; } = "";
    public decimal PrecioUnitario { get; set; }
    public int Cantidad { get; set; }
    public decimal Subtotal { get; set; }
}

public class Notificacion
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Titulo { get; set; } = "";
    public string Mensaje { get; set; } = "";
    public bool Leida { get; set; }
    public DateTime CreatedAt { get; set; }
}
