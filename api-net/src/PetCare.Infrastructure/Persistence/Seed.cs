using Microsoft.EntityFrameworkCore;
using PetCare.Application.Abstracciones;
using PetCare.Domain;

namespace PetCare.Infrastructure.Persistence;

public class Seed(PetCareDbContext db, ISeguridad seguridad)
{
    private static readonly string[] Especies = ["Perro", "Gato", "Ave", "Conejo", "Otro"];

    private static readonly (string Nombre, int Intervalo)[] Vacunas =
    [("Antirrábica", 365), ("Séxtuple", 365), ("Desparasitación", 90)];

    private static readonly (string Nombre, decimal Precio, int Duracion)[] Servicios =
    [
        ("Consulta general", 350, 30),
        ("Vacuna antirrábica", 300, 30),
        ("Vacuna séxtuple", 450, 30),
        ("Desparasitación", 200, 30),
        ("Baño y peluquería", 400, 60),
    ];

    private static readonly (string Nombre, string Categoria, decimal Precio, int Stock)[] Productos =
    [
        ("Alimento para perro adulto 15 kg", "Alimento", 1250, 20),
        ("Alimento para gato adulto 3 kg", "Alimento", 480, 25),
        ("Collar ajustable", "Accesorios", 150, 40),
        ("Correa retráctil 5 m", "Accesorios", 320, 15),
        ("Shampoo para mascotas", "Higiene", 210, 30),
        ("Juguete de goma", "Juguetes", 95, 50),
    ];

    public async Task EjecutarAsync(string emailAdmin, string claveAdmin, string claveDemo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(claveAdmin))
            throw new InvalidOperationException("Define Seed:AdminPassword antes de ejecutar el seed");

        await CargarRolesAsync(ct);
        await CargarCatalogosAsync(ct);

        await CrearUsuarioAsync("Administrador", emailAdmin, claveAdmin, Roles.Administrador, null, ct);
        var vet = await CrearUsuarioAsync("Dra. Ana Martínez", "veterinario@demo.hn", claveDemo, Roles.Veterinario, "9999-1111", ct);
        await FijarHorariosAsync(vet, Semana().Append(new VeterinarioHorario { DiaSemana = 6, HoraInicio = new TimeOnly(8, 0), HoraFin = new TimeOnly(12, 0) }), ct);
        var vet2 = await CrearUsuarioAsync("Dr. Carlos Reyes", "veterinario2@demo.hn", claveDemo, Roles.Veterinario, "9999-2222", ct);
        await FijarHorariosAsync(vet2, Semana(), ct);
        var dueno = await CrearUsuarioAsync("Cliente Demo", "dueno@demo.hn", claveDemo, Roles.Dueno, "9999-0000", ct);
        await SembrarMascotaDemoAsync(dueno, vet.Id, ct);

        Console.WriteLine("Datos iniciales cargados.");
    }

    private async Task CargarRolesAsync(CancellationToken ct)
    {
        foreach (var nombre in new[] { Roles.Dueno, Roles.Veterinario, Roles.Administrador })
        {
            if (!await db.Roles.AnyAsync(r => r.Nombre == nombre, ct)) db.Roles.Add(new Rol { Nombre = nombre });
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task CargarCatalogosAsync(CancellationToken ct)
    {
        foreach (var nombre in Especies)
            if (!await db.Especies.AnyAsync(e => e.Nombre == nombre, ct)) db.Especies.Add(new Especie { Nombre = nombre });
        foreach (var (nombre, intervalo) in Vacunas)
            if (!await db.Vacunas.AnyAsync(v => v.Nombre == nombre, ct)) db.Vacunas.Add(new Vacuna { Nombre = nombre, IntervaloDias = intervalo });
        foreach (var (nombre, precio, duracion) in Servicios)
            if (!await db.Servicios.AnyAsync(s => s.Nombre == nombre, ct)) db.Servicios.Add(new Servicio { Nombre = nombre, Precio = precio, DuracionMin = duracion });
        foreach (var (nombre, categoria, precio, stock) in Productos)
            if (!await db.Productos.AnyAsync(p => p.Nombre == nombre, ct))
                db.Productos.Add(new Producto { Nombre = nombre, Categoria = categoria, Precio = precio, Stock = stock });
        await db.SaveChangesAsync(ct);
    }

    private async Task<Usuario> CrearUsuarioAsync(string nombre, string email, string clave, string rol, string? telefono, CancellationToken ct)
    {
        var existente = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (existente is not null) return existente;
        var rolId = await db.Roles.Where(r => r.Nombre == rol).Select(r => r.Id).SingleAsync(ct);
        var usuario = new Usuario
        {
            RolId = rolId,
            Nombre = nombre,
            Email = email,
            Telefono = telefono,
            PasswordHash = seguridad.HashPassword(clave),
            EmailVerificado = true,
            CreatedAt = DateTime.Now,
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync(ct);
        return usuario;
    }

    private async Task FijarHorariosAsync(Usuario vet, IEnumerable<VeterinarioHorario> horarios, CancellationToken ct)
    {
        await db.VeterinarioHorarios.Where(h => h.VeterinarioId == vet.Id).ExecuteDeleteAsync(ct);
        var lista = horarios.ToList();
        foreach (var h in lista) h.VeterinarioId = vet.Id;
        db.VeterinarioHorarios.AddRange(lista);
        await db.SaveChangesAsync(ct);
    }

    private async Task SembrarMascotaDemoAsync(Usuario dueno, int vetId, CancellationToken ct)
    {
        if (await db.Mascotas.AnyAsync(m => m.DuenoId == dueno.Id && m.Nombre == "Firulais", ct)) return;

        var perroId = await db.Especies.Where(e => e.Nombre == "Perro").Select(e => e.Id).SingleAsync(ct);
        var antirrabica = await db.Vacunas.SingleAsync(v => v.Nombre == "Antirrábica", ct);
        var mascota = new Mascota
        {
            DuenoId = dueno.Id,
            EspecieId = perroId,
            Nombre = "Firulais",
            Raza = "Labrador",
            FechaNacimiento = new DateOnly(2020, 3, 15),
            PesoKg = 25.5m,
        };
        db.Mascotas.Add(mascota);
        await db.SaveChangesAsync(ct);

        var fecha = new DateOnly(2026, 1, 10);
        db.AplicacionesVacuna.Add(new AplicacionVacuna
        {
            MascotaId = mascota.Id,
            VacunaId = antirrabica.Id,
            VeterinarioId = vetId,
            FechaAplicacion = fecha,
            ProximaDosis = fecha.AddDays(antirrabica.IntervaloDias),
            Lote = "LOTE-DEMO",
        });
        await db.SaveChangesAsync(ct);
    }

    private static IEnumerable<VeterinarioHorario> Semana() =>
        Enumerable.Range(1, 5).Select(d => new VeterinarioHorario
        {
            DiaSemana = d,
            HoraInicio = new TimeOnly(8, 0),
            HoraFin = new TimeOnly(17, 0),
        });
}
