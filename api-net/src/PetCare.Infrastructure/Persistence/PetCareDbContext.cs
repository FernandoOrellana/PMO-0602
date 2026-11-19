using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PetCare.Domain;

namespace PetCare.Infrastructure.Persistence;

public class PetCareDbContext(DbContextOptions<PetCareDbContext> options) : DbContext(options)
{
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Dispositivo> Dispositivos => Set<Dispositivo>();
    public DbSet<Especie> Especies => Set<Especie>();
    public DbSet<Vacuna> Vacunas => Set<Vacuna>();
    public DbSet<Servicio> Servicios => Set<Servicio>();
    public DbSet<VeterinarioHorario> VeterinarioHorarios => Set<VeterinarioHorario>();
    public DbSet<Mascota> Mascotas => Set<Mascota>();
    public DbSet<Cita> Citas => Set<Cita>();
    public DbSet<Consulta> Consultas => Set<Consulta>();
    public DbSet<AplicacionVacuna> AplicacionesVacuna => Set<AplicacionVacuna>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<PedidoItem> PedidoItems => Set<PedidoItem>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        Tabla<Rol>(b, "roles");
        Tabla<Usuario>(b, "usuarios");
        Tabla<RefreshToken>(b, "refresh_tokens");
        Tabla<Dispositivo>(b, "dispositivos");
        Tabla<Especie>(b, "especies");
        Tabla<Vacuna>(b, "vacunas");
        Tabla<Servicio>(b, "servicios");
        Tabla<VeterinarioHorario>(b, "veterinario_horarios");
        Tabla<Mascota>(b, "mascotas");
        Tabla<Cita>(b, "citas");
        Tabla<Consulta>(b, "consultas");
        Tabla<AplicacionVacuna>(b, "aplicaciones_vacuna");
        Tabla<Producto>(b, "productos");
        Tabla<Pedido>(b, "pedidos");
        Tabla<PedidoItem>(b, "pedido_items");
        Tabla<Notificacion>(b, "notificaciones");

        b.Entity<Usuario>(e =>
        {
            e.HasOne(u => u.Rol).WithMany().HasForeignKey(u => u.RolId);
            e.HasMany(u => u.Horarios).WithOne().HasForeignKey(h => h.VeterinarioId);
        });
        b.Entity<Mascota>(e =>
        {
            e.HasOne(m => m.Dueno).WithMany().HasForeignKey(m => m.DuenoId);
            e.HasOne(m => m.Especie).WithMany().HasForeignKey(m => m.EspecieId);
            e.Property(m => m.PesoKg).HasPrecision(6, 2);
        });
        b.Entity<Cita>(e =>
        {
            e.HasOne(c => c.Mascota).WithMany().HasForeignKey(c => c.MascotaId);
            e.HasOne(c => c.Servicio).WithMany().HasForeignKey(c => c.ServicioId);
            e.HasOne(c => c.Veterinario).WithMany().HasForeignKey(c => c.VeterinarioId);
            e.Property(c => c.Estado).HasConversion<string>();
        });
        b.Entity<Consulta>(e =>
        {
            e.HasOne(c => c.Veterinario).WithMany().HasForeignKey(c => c.VeterinarioId);
            e.Property(c => c.PesoKg).HasPrecision(6, 2);
        });
        b.Entity<AplicacionVacuna>(e =>
        {
            e.HasOne(a => a.Mascota).WithMany().HasForeignKey(a => a.MascotaId);
            e.HasOne(a => a.Vacuna).WithMany().HasForeignKey(a => a.VacunaId);
            e.HasOne(a => a.Veterinario).WithMany().HasForeignKey(a => a.VeterinarioId);
        });
        b.Entity<Servicio>(e => e.Property(s => s.Precio).HasPrecision(10, 2));
        b.Entity<Producto>(e => e.Property(p => p.Precio).HasPrecision(10, 2));
        b.Entity<Pedido>(e =>
        {
            e.HasOne(p => p.Cliente).WithMany().HasForeignKey(p => p.ClienteId);
            e.HasMany(p => p.Items).WithOne(i => i.Pedido).HasForeignKey(i => i.PedidoId);
            e.Property(p => p.Total).HasPrecision(10, 2);
            e.Property(p => p.Estado).HasConversion<string>();
        });
        b.Entity<PedidoItem>(e =>
        {
            e.HasOne(i => i.Producto).WithMany().HasForeignKey(i => i.ProductoId);
            e.Property(i => i.PrecioUnitario).HasPrecision(10, 2);
            e.Property(i => i.Subtotal).HasPrecision(10, 2);
        });

        var conversorHora = new ValueConverter<TimeOnly, TimeSpan>(h => h.ToTimeSpan(), t => TimeOnly.FromTimeSpan(t));
        var conversorFecha = new ValueConverter<DateOnly, DateTime>(
            d => d.ToDateTime(TimeOnly.MinValue),
            dt => DateOnly.FromDateTime(dt));
        var conversorFechaNullable = new ValueConverter<DateOnly?, DateTime?>(
            d => d.HasValue ? d.Value.ToDateTime(TimeOnly.MinValue) : null,
            dt => dt.HasValue ? DateOnly.FromDateTime(dt.Value) : null);

        b.Entity<VeterinarioHorario>(e =>
        {
            e.Property(h => h.HoraInicio).HasConversion(conversorHora);
            e.Property(h => h.HoraFin).HasConversion(conversorHora);
        });
        b.Entity<Mascota>(e => e.Property(m => m.FechaNacimiento).HasConversion(conversorFechaNullable));
        b.Entity<Cita>(e =>
        {
            e.Property(c => c.Hora).HasConversion(conversorHora);
            e.Property(c => c.Fecha).HasConversion(conversorFecha);
        });
        b.Entity<Consulta>(e => e.Property(c => c.Fecha).HasConversion(conversorFecha));
        b.Entity<AplicacionVacuna>(e =>
        {
            e.Property(a => a.FechaAplicacion).HasConversion(conversorFecha);
            e.Property(a => a.ProximaDosis).HasConversion(conversorFechaNullable);
        });
    }

    private static void Tabla<T>(ModelBuilder b, string nombre) where T : class
    {
        b.Entity<T>(e =>
        {
            e.ToTable(nombre);
            foreach (var prop in e.Metadata.GetProperties())
                prop.SetColumnName(ASnakeCase(prop.Name));
        });
    }

    private static string ASnakeCase(string nombre) =>
        string.Concat(nombre.Select((c, i) => char.IsUpper(c) && i > 0 ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
}
