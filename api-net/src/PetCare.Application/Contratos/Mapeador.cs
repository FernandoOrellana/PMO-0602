using PetCare.Application.Abstracciones;
using PetCare.Domain;

namespace PetCare.Application.Contratos;

public class Mapeador(IAlmacenArchivos almacen)
{
    private const string FormatoHora = "HH:mm";

    public UsuarioDto Usuario(PetCare.Domain.Usuario u) =>
        new(u.Id, u.Nombre, u.Email, u.Telefono, u.Rol.Nombre);

    public EspecieDto Especie(PetCare.Domain.Especie e) => new(e.Id, e.Nombre);

    public VacunaDto Vacuna(PetCare.Domain.Vacuna v) => new(v.Id, v.Nombre, v.IntervaloDias);

    public ServicioDto Servicio(PetCare.Domain.Servicio s) => new(s.Id, s.Nombre, s.Precio, s.DuracionMin, s.Activo);

    public VeterinarioDto Veterinario(PetCare.Domain.Usuario v) =>
        new(v.Id, v.Nombre, v.Email, v.Telefono,
            v.Horarios.OrderBy(h => h.DiaSemana).ThenBy(h => h.HoraInicio)
                .Select(h => new HorarioDto(h.DiaSemana, h.HoraInicio.ToString(FormatoHora), h.HoraFin.ToString(FormatoHora)))
                .ToList());

    public MascotaDto Mascota(PetCare.Domain.Mascota m) =>
        new(m.Id, m.DuenoId, m.Nombre, new ResumenDto(m.Especie.Id, m.Especie.Nombre), m.Raza,
            m.FechaNacimiento, m.PesoKg, almacen.UrlPublica(m.FotoRuta), m.Activo);

    public AplicacionVacunaDto Aplicacion(AplicacionVacuna a) =>
        new(a.Id, new ResumenDto(a.Vacuna.Id, a.Vacuna.Nombre), a.FechaAplicacion, a.ProximaDosis, a.Lote,
            new ResumenDto(a.Veterinario.Id, a.Veterinario.Nombre));

    public ConsultaDto Consulta(PetCare.Domain.Consulta c) =>
        new(c.Id, c.CitaId, c.Fecha, c.Diagnostico, c.Tratamiento, c.Observaciones, c.PesoKg,
            new ResumenDto(c.Veterinario.Id, c.Veterinario.Nombre));

    public CitaDto Cita(PetCare.Domain.Cita c) =>
        new(c.Id, c.Fecha, c.Hora.ToString(FormatoHora), c.Estado.ToString(),
            new ResumenDto(c.Mascota.Id, c.Mascota.Nombre),
            new ServicioResumenDto(c.Servicio.Id, c.Servicio.Nombre, c.Servicio.Precio),
            new ResumenDto(c.Veterinario.Id, c.Veterinario.Nombre),
            new DuenoDto(c.Mascota.Dueno.Id, c.Mascota.Dueno.Nombre, c.Mascota.Dueno.Telefono));

    public ProductoDto Producto(PetCare.Domain.Producto p) =>
        new(p.Id, p.Nombre, p.Descripcion, p.Categoria, p.Precio, p.Stock, almacen.UrlPublica(p.ImagenRuta), p.Activo);

    public PedidoDto Pedido(PetCare.Domain.Pedido p) =>
        new(p.Id, p.Fecha, p.Estado.ToString(), p.Total,
            p.Items.Select(i => new PedidoItemDto(i.ProductoId, i.NombreProducto, i.PrecioUnitario, i.Cantidad, i.Subtotal)).ToList(),
            new ResumenDto(p.Cliente.Id, p.Cliente.Nombre));

    public NotificacionDto Notificacion(PetCare.Domain.Notificacion n) =>
        new(n.Id, n.Titulo, n.Mensaje, n.Leida, n.CreatedAt);
}
