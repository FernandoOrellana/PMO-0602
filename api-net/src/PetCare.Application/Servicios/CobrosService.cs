using System.Text.RegularExpressions;
using PetCare.Application.Abstracciones;
using PetCare.Application.Common;
using PetCare.Application.Contratos;
using PetCare.Domain;

namespace PetCare.Application.Servicios;

public class CobrosService(ICobroRepositorio cobros, IPedidoRepositorio pedidos, ICitaRepositorio citas,
    IUsuarioRepositorio usuarios, IReloj reloj)
{
    public Task<ResultadoCobro> CobrarPedidoAsync(Actor actor, int id, RegistrarCobroRequest request, CancellationToken ct) =>
        RegistrarAsync(actor, true, id, request, ct);

    public Task<ResultadoCobro> CobrarCitaAsync(Actor actor, int id, RegistrarCobroRequest request, CancellationToken ct) =>
        RegistrarAsync(actor, false, id, request, ct);

    public async Task<ComprobanteInternoDto> ObtenerAsync(Actor actor, int id, CancellationToken ct)
    {
        Autorizar(actor);
        return Comprobante(await cobros.PorIdAsync(id, ct) ?? throw new NoEncontradoException("Cobro no encontrado"));
    }

    public async Task<ComprobanteInternoDto> ObtenerPorOrigenAsync(Actor actor, bool esPedido, int id, CancellationToken ct)
    {
        Autorizar(actor);
        return Comprobante(await cobros.PorOrigenAsync(esPedido, id, ct) ?? throw new NoEncontradoException("Cobro no encontrado"));
    }

    private async Task<ResultadoCobro> RegistrarAsync(Actor actor, bool esPedido, int id, RegistrarCobroRequest request, CancellationToken ct)
    {
        Autorizar(actor);
        if (id <= 0 || request is null) throw new ValidacionException("Datos de cobro inválidos");
        if (!Enum.TryParse<MetodoCobro>(request.Metodo, false, out var metodo) ||
            request.Metodo != metodo.ToString() || !Enum.IsDefined(metodo))
            throw new ValidacionException("El método debe ser EFECTIVO o TRANSFERENCIA");
        if (request.ClaveIdempotencia is null || !Regex.IsMatch(request.ClaveIdempotencia, @"\A[A-Za-z0-9_-]{16,64}\z"))
            throw new ValidacionException("La clave de idempotencia debe contener entre 16 y 64 caracteres ASCII permitidos");
        var referencia = string.IsNullOrWhiteSpace(request.ReferenciaTransferencia) ? null : request.ReferenciaTransferencia.Trim();
        if (referencia is not null && (metodo != MetodoCobro.TRANSFERENCIA || referencia.Length > 120 || referencia.Any(char.IsControl)))
            throw new ValidacionException("Referencia de transferencia inválida");

        return await cobros.EnTransaccionAsync(esPedido, id, async () =>
        {
            var previo = await cobros.PorClaveAsync(request.ClaveIdempotencia, ct);
            if (previo is not null)
            {
                if (previo.PedidoId != (esPedido ? id : null) || previo.CitaId != (esPedido ? null : id) ||
                    previo.Metodo != metodo || previo.ReferenciaTransferencia != referencia || previo.AdministradorId != actor.Id)
                    throw new ConflictoException("La clave de idempotencia pertenece a otra operación");
                return new ResultadoCobro(Comprobante(previo), false);
            }
            if (await cobros.PorOrigenAsync(esPedido, id, ct) is not null)
                throw new ConflictoException("El pedido o cita ya tiene un cobro registrado");
            var administrador = await usuarios.BuscarPorIdAsync(actor.Id, ct) ?? throw new NoEncontradoException("Administrador no encontrado");
            var cobro = new Cobro { AdministradorId = actor.Id, NombreAdministrador = administrador.Nombre, Fecha = reloj.Ahora,
                Metodo = metodo, ReferenciaTransferencia = referencia, ClaveIdempotencia = request.ClaveIdempotencia };
            if (esPedido)
            {
                var pedido = await pedidos.PorIdAsync(id, ct) ?? throw new NoEncontradoException("Pedido no encontrado");
                if (pedido.Estado != EstadoPedido.LISTO_PARA_RECOGER) throw new ConflictoException("Solo se pueden cobrar pedidos listos para recoger");
                cobro.PedidoId = id; cobro.ClienteId = pedido.ClienteId; cobro.NombreCliente = pedido.Cliente.Nombre;
                cobro.Items = pedido.Items.Select(i => new CobroItem { Descripcion = i.NombreProducto, Cantidad = i.Cantidad,
                    PrecioUnitario = i.PrecioUnitario, Subtotal = i.Subtotal }).ToList();
                cobro.Importe = pedido.Total;
                if (cobro.Items.Count == 0 || Reglas.Total(cobro.Items.Select(i => i.Subtotal)) != cobro.Importe)
                    throw new ConflictoException("El total del pedido no coincide con sus productos");
            }
            else
            {
                var cita = await citas.PorIdAsync(id, ct) ?? throw new NoEncontradoException("Cita no encontrada");
                if (cita.Estado != EstadoCita.ATENDIDA) throw new ConflictoException("Solo se pueden cobrar citas atendidas");
                // Las citas anteriores no tienen precio histórico: se usa y congela el precio vigente al cobrar.
                var precio = cita.PrecioServicioHistorico ?? cita.Servicio.Precio;
                if (precio < 0) throw new ConflictoException("El precio del servicio no es válido");
                cobro.CitaId = id; cobro.ClienteId = cita.Mascota.DuenoId; cobro.NombreCliente = cita.Mascota.Dueno.Nombre;
                cobro.Importe = Reglas.Redondear(precio);
                cobro.Items = [new CobroItem { Descripcion = cita.Servicio.Nombre, Cantidad = 1, PrecioUnitario = cobro.Importe, Subtotal = cobro.Importe }];
            }
            cobros.Agregar(cobro);
            await cobros.GuardarAsync(ct);
            return new ResultadoCobro(Comprobante(cobro), true);
        }, ct);
    }

    private static void Autorizar(Actor actor)
    {
        if (actor.Rol != Roles.Administrador) throw new ProhibidoException();
    }

    private static ComprobanteInternoDto Comprobante(Cobro c) => new(c.Id, $"COM-{c.Id}", c.PedidoId, c.CitaId,
        c.Fecha, new ResumenDto(c.ClienteId, c.NombreCliente), new ResumenDto(c.AdministradorId, c.NombreAdministrador),
        c.Metodo.ToString(), c.ReferenciaTransferencia, c.Importe,
        c.Items.Select(i => new CobroItemDto(i.Descripcion, i.Cantidad, i.PrecioUnitario, i.Subtotal)).ToList());
}
