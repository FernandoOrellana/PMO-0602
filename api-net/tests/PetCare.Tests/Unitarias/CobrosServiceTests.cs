using Microsoft.AspNetCore.Authorization;
using Moq;
using PetCare.Api.Controllers;
using PetCare.Application.Abstracciones;
using PetCare.Application.Common;
using PetCare.Application.Contratos;
using PetCare.Application.Servicios;
using PetCare.Domain;

namespace PetCare.Tests.Unitarias;

public class CobrosServiceTests
{
    private readonly Mock<ICobroRepositorio> _cobros = new();
    private readonly Mock<IPedidoRepositorio> _pedidos = new();
    private readonly Mock<ICitaRepositorio> _citas = new();
    private readonly Mock<IUsuarioRepositorio> _usuarios = new();
    private readonly Actor _admin = new(1, Roles.Administrador);
    private static RegistrarCobroRequest Solicitud(string metodo = "EFECTIVO", string? referencia = null, string clave = "idempotencia_12345") => new(metodo, referencia, clave);

    public CobrosServiceTests()
    {
        _cobros.Setup(r => r.EnTransaccionAsync(It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<Func<Task<ResultadoCobro>>>(), It.IsAny<CancellationToken>()))
            .Returns<bool, int, Func<Task<ResultadoCobro>>, CancellationToken>((_, _, operacion, _) => operacion());
        _usuarios.Setup(r => r.BuscarPorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Datos.Usuario(1, Roles.Administrador));
        _cobros.Setup(r => r.Agregar(It.IsAny<Cobro>())).Callback<Cobro>(c => c.Id = 9);
    }

    private CobrosService Crear() => new(_cobros.Object, _pedidos.Object, _citas.Object, _usuarios.Object,
        Mocks.Reloj(new DateTime(2030, 1, 1)).Object);

    private Pedido Pedido(EstadoPedido estado = EstadoPedido.LISTO_PARA_RECOGER) => new()
    {
        Id = 7, ClienteId = 5, Cliente = Datos.Usuario(5, Roles.Dueno), Estado = estado, Total = 25m,
        Items = [new PedidoItem { NombreProducto = "Producto histórico", Cantidad = 2, PrecioUnitario = 12.5m, Subtotal = 25m }],
    };

    [Fact]
    public async Task Pedido_UsaImporteYDetallePersistidos()
    {
        _pedidos.Setup(r => r.PorIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(Pedido());
        var resultado = await Crear().CobrarPedidoAsync(_admin, 7, Solicitud(), default);
        Assert.True(resultado.Creado);
        Assert.Equal(25m, resultado.Comprobante.Importe);
        Assert.Equal("Producto histórico", Assert.Single(resultado.Comprobante.Items).Descripcion);
        _cobros.Verify(r => r.GuardarAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(EstadoPedido.PENDIENTE)]
    [InlineData(EstadoPedido.CANCELADO)]
    [InlineData(EstadoPedido.ENTREGADO)]
    public async Task Pedido_RechazaEstadosNoCobrables(EstadoPedido estado)
    {
        _pedidos.Setup(r => r.PorIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(Pedido(estado));
        await Assert.ThrowsAsync<ConflictoException>(() => Crear().CobrarPedidoAsync(_admin, 7, Solicitud(), default));
        _cobros.Verify(r => r.Agregar(It.IsAny<Cobro>()), Times.Never);
    }

    [Theory]
    [InlineData(EstadoCita.PENDIENTE)]
    [InlineData(EstadoCita.CONFIRMADA)]
    [InlineData(EstadoCita.CANCELADA)]
    public async Task Cita_RechazaEstadosNoCobrables(EstadoCita estado)
    {
        _citas.Setup(r => r.PorIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(new Cita { Estado = estado });
        await Assert.ThrowsAsync<ConflictoException>(() => Crear().CobrarCitaAsync(_admin, 7, Solicitud(), default));
    }

    [Theory]
    [InlineData(true, 80)]
    [InlineData(false, 350)]
    public async Task Cita_UsaPrecioHistoricoOVigenteSiEsAnterior(bool tieneHistorico, int esperado)
    {
        var cita = new Cita { Estado = EstadoCita.ATENDIDA, Mascota = Datos.Mascota(1, 5), Servicio = Datos.Servicio(), PrecioServicioHistorico = tieneHistorico ? 80m : null };
        _citas.Setup(r => r.PorIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(cita);
        var resultado = await Crear().CobrarCitaAsync(_admin, 7, Solicitud("TRANSFERENCIA", "REF-123"), default);
        Assert.Equal((decimal)esperado, resultado.Comprobante.Importe);
        Assert.Equal("REF-123", resultado.Comprobante.ReferenciaTransferencia);
    }

    [Theory]
    [InlineData("TARJETA", null, "idempotencia_12345")]
    [InlineData("EFECTIVO", "REF", "idempotencia_12345")]
    [InlineData("EFECTIVO", null, "corta")]
    public async Task DatosInvalidosSonRechazados(string metodo, string? referencia, string clave) =>
        await Assert.ThrowsAsync<ValidacionException>(() => Crear().CobrarPedidoAsync(_admin, 7, Solicitud(metodo, referencia, clave), default));

    [Fact]
    public async Task ReintentoIdenticoDevuelveElMismoComprobante()
    {
        _cobros.Setup(r => r.PorClaveAsync("idempotencia_12345", It.IsAny<CancellationToken>())).ReturnsAsync(new Cobro
        { Id = 9, PedidoId = 7, AdministradorId = 1, Metodo = MetodoCobro.EFECTIVO, ClaveIdempotencia = "idempotencia_12345" });
        var resultado = await Crear().CobrarPedidoAsync(_admin, 7, Solicitud(), default);
        Assert.False(resultado.Creado);
        Assert.Equal(9, resultado.Comprobante.Id);
        _cobros.Verify(r => r.GuardarAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ClaveReutilizadaConOtrosDatosEsConflicto()
    {
        _cobros.Setup(r => r.PorClaveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Cobro
        { PedidoId = 7, AdministradorId = 1, Metodo = MetodoCobro.TRANSFERENCIA });
        await Assert.ThrowsAsync<ConflictoException>(() => Crear().CobrarPedidoAsync(_admin, 7, Solicitud(), default));
    }

    [Fact]
    public async Task OrigenYaCobradoEsConflicto()
    {
        _cobros.Setup(r => r.PorOrigenAsync(true, 7, It.IsAny<CancellationToken>())).ReturnsAsync(new Cobro());
        await Assert.ThrowsAsync<ConflictoException>(() => Crear().CobrarPedidoAsync(_admin, 7, Solicitud(), default));
    }

    [Fact]
    public async Task PedidoConTotalInconsistenteNoSeGuarda()
    {
        var pedido = Pedido();
        pedido.Total = 30m;
        _pedidos.Setup(r => r.PorIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(pedido);
        var error = await Assert.ThrowsAsync<ConflictoException>(() => Crear().CobrarPedidoAsync(_admin, 7, Solicitud(), default));
        Assert.Equal(409, error.Status);
        _cobros.Verify(r => r.GuardarAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ComprobanteInexistenteRespondeNoEncontrado()
    {
        var error = await Assert.ThrowsAsync<NoEncontradoException>(() => Crear().ObtenerAsync(_admin, 123, default));
        Assert.Equal(404, error.Status);
    }

    [Fact]
    public async Task SoloAdministradorPuedeCobrarYControladorExigeRol()
    {
        await Assert.ThrowsAsync<ProhibidoException>(() => Crear().CobrarPedidoAsync(new Actor(5, Roles.Dueno), 7, Solicitud(), default));
        var atributo = Assert.Single(typeof(CobrosController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal(Roles.Administrador, atributo.Roles);
    }
}
