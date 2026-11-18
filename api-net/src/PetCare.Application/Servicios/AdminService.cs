using PetCare.Application.Abstracciones;
using PetCare.Application.Common;
using PetCare.Application.Contratos;
using PetCare.Domain;

namespace PetCare.Application.Servicios;

public class AdminService(
    IProductoRepositorio productos,
    ICatalogoRepositorio catalogo,
    IVeterinarioRepositorio veterinarios,
    IUsuarioRepositorio usuarios,
    IReporteRepositorio reportes,
    ISeguridad seguridad,
    IAlmacenArchivos almacen,
    IUnitOfWork uow,
    IReloj reloj,
    Mapeador mapeador)
{
    public async Task<ProductoDto> CrearProductoAsync(ProductoRequest r, CancellationToken ct)
    {
        var producto = new Producto
        {
            Nombre = r.Nombre.Trim(),
            Descripcion = r.Descripcion,
            Categoria = r.Categoria.Trim(),
            Precio = r.Precio,
            Stock = r.Stock,
        };
        productos.Agregar(producto);
        await uow.GuardarAsync(ct);
        return mapeador.Producto(producto);
    }

    public async Task<ProductoDto> ActualizarProductoAsync(int id, ProductoRequest r, CancellationToken ct)
    {
        var producto = await CargarProducto(id, ct);
        producto.Nombre = r.Nombre.Trim();
        producto.Descripcion = r.Descripcion;
        producto.Categoria = r.Categoria.Trim();
        producto.Precio = r.Precio;
        producto.Stock = r.Stock;
        await uow.GuardarAsync(ct);
        return mapeador.Producto(producto);
    }

    public async Task<MensajeDto> DesactivarProductoAsync(int id, CancellationToken ct)
    {
        var producto = await CargarProducto(id, ct);
        producto.Activo = false;
        await uow.GuardarAsync(ct);
        return new MensajeDto("Producto desactivado");
    }

    public async Task<ProductoDto> ActualizarImagenProductoAsync(int id, Stream contenido, string extension, CancellationToken ct)
    {
        var producto = await CargarProducto(id, ct);
        var anterior = producto.ImagenRuta;
        producto.ImagenRuta = await almacen.GuardarAsync("productos", contenido, extension, ct);
        await uow.GuardarAsync(ct);
        await almacen.EliminarAsync(anterior, ct);
        return mapeador.Producto(producto);
    }

    public async Task<ServicioDto> CrearServicioAsync(ServicioRequest r, CancellationToken ct)
    {
        var servicio = new Servicio { Nombre = r.Nombre.Trim(), Precio = r.Precio, DuracionMin = r.DuracionMin };
        catalogo.AgregarServicio(servicio);
        await uow.GuardarAsync(ct);
        return mapeador.Servicio(servicio);
    }

    public async Task<ServicioDto> ActualizarServicioAsync(int id, ServicioRequest r, CancellationToken ct)
    {
        var servicio = await CargarServicio(id, ct);
        servicio.Nombre = r.Nombre.Trim();
        servicio.Precio = r.Precio;
        servicio.DuracionMin = r.DuracionMin;
        await uow.GuardarAsync(ct);
        return mapeador.Servicio(servicio);
    }

    public async Task<MensajeDto> DesactivarServicioAsync(int id, CancellationToken ct)
    {
        var servicio = await CargarServicio(id, ct);
        servicio.Activo = false;
        await uow.GuardarAsync(ct);
        return new MensajeDto("Servicio desactivado");
    }

    public async Task<VeterinarioDto> CrearVeterinarioAsync(VeterinarioCrearRequest r, CancellationToken ct)
    {
        var email = r.Email.Trim().ToLowerInvariant();
        if (await usuarios.BuscarPorEmailAsync(email, ct) is not null)
            throw new ConflictoException("Ya existe una cuenta con ese correo");

        var usuario = new Usuario
        {
            RolId = await usuarios.IdRolAsync(Roles.Veterinario, ct),
            Nombre = r.Nombre.Trim(),
            Email = email,
            Telefono = r.Telefono,
            PasswordHash = seguridad.HashPassword(r.Password),
            EmailVerificado = true,
            CreatedAt = reloj.Ahora,
        };
        usuarios.Agregar(usuario);
        await uow.GuardarAsync(ct);
        return mapeador.Veterinario(await CargarVeterinario(usuario.Id, ct));
    }

    public async Task<VeterinarioDto> ActualizarVeterinarioAsync(int id, VeterinarioActualizarRequest r, CancellationToken ct)
    {
        var vet = await CargarVeterinario(id, ct);
        vet.Nombre = r.Nombre.Trim();
        vet.Telefono = r.Telefono;
        await uow.GuardarAsync(ct);
        return mapeador.Veterinario(vet);
    }

    public async Task<VeterinarioDto> ReemplazarHorariosAsync(int id, IReadOnlyList<HorarioRequest> horarios, CancellationToken ct)
    {
        var vet = await CargarVeterinario(id, ct);
        vet.Horarios.Clear();
        foreach (var h in horarios)
        {
            vet.Horarios.Add(new VeterinarioHorario
            {
                VeterinarioId = id,
                DiaSemana = h.DiaSemana,
                HoraInicio = TimeOnly.ParseExact(h.HoraInicio, "HH:mm"),
                HoraFin = TimeOnly.ParseExact(h.HoraFin, "HH:mm"),
            });
        }
        await uow.GuardarAsync(ct);
        return mapeador.Veterinario(vet);
    }

    public async Task<ReporteVentasDto> ReporteVentasAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct)
    {
        var (d, h) = Rango(desde, hasta);
        var resumen = await reportes.VentasAsync(d, h, ct);
        return new ReporteVentasDto(d, h, resumen.TotalPedidos, resumen.TotalVentas, resumen.Productos);
    }

    public async Task<ReporteCitasDto> ReporteCitasAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct)
    {
        var (d, h) = Rango(desde, hasta);
        var resumen = await reportes.CitasAsync(d, h, ct);
        return new ReporteCitasDto(d, h, resumen.Total, resumen.PorEstado, resumen.PorServicio);
    }

    private (DateOnly Desde, DateOnly Hasta) Rango(DateOnly? desde, DateOnly? hasta)
    {
        var fin = hasta ?? DateOnly.FromDateTime(reloj.Ahora);
        var inicio = desde ?? new DateOnly(fin.Year, fin.Month, 1);
        if (inicio > fin) throw new ValidacionException("La fecha inicial no puede ser mayor que la final");
        return (inicio, fin);
    }

    private async Task<Producto> CargarProducto(int id, CancellationToken ct) =>
        await productos.PorIdAsync(id, ct) ?? throw new NoEncontradoException("Producto no encontrado");

    private async Task<Servicio> CargarServicio(int id, CancellationToken ct) =>
        await catalogo.ServicioPorIdAsync(id, ct) ?? throw new NoEncontradoException("Servicio no encontrado");

    private async Task<Usuario> CargarVeterinario(int id, CancellationToken ct) =>
        await veterinarios.PorIdAsync(id, ct) ?? throw new NoEncontradoException("Veterinario no encontrado");
}
