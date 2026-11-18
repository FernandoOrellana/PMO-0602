using PetCare.Domain;

namespace PetCare.Tests.Unitarias;

public class ReglasTests
{
    private static readonly DateOnly Lunes = new(2030, 1, 7);
    private static readonly DateTime AhoraLejano = new(2030, 1, 1, 10, 0, 0);
    private static readonly Franja[] Horario = [new(1, new TimeOnly(8, 0), new TimeOnly(10, 0))];

    [Fact]
    public void HorasDisponibles_GeneraTramosDe30Minutos()
    {
        var horas = Reglas.HorasDisponibles(Lunes, Horario, [], 30, AhoraLejano);
        Assert.Equal(["08:00", "08:30", "09:00", "09:30"], horas.Select(h => h.ToString("HH:mm")));
    }

    [Fact]
    public void HorasDisponibles_ExcluyeHorasOcupadasConsiderandoLaDuracion()
    {
        var ocupadas = new[] { new Ocupacion(new TimeOnly(8, 30), 60) };
        var horas = Reglas.HorasDisponibles(Lunes, Horario, ocupadas, 30, AhoraLejano);
        Assert.Equal(["08:00", "09:30"], horas.Select(h => h.ToString("HH:mm")));
    }

    [Fact]
    public void HorasDisponibles_NoOfreceHorasQueYaPasaronHoy()
    {
        var horas = Reglas.HorasDisponibles(Lunes, Horario, [], 30, new DateTime(2030, 1, 7, 8, 45, 0));
        Assert.Equal(["09:00", "09:30"], horas.Select(h => h.ToString("HH:mm")));
    }

    [Fact]
    public void EsFechaHoraFutura_RechazaFechasPasadas()
    {
        var ahora = new DateTime(2030, 1, 7, 9, 0, 0);
        Assert.False(Reglas.EsFechaHoraFutura(new DateOnly(2030, 1, 6), new TimeOnly(10, 0), ahora));
        Assert.False(Reglas.EsFechaHoraFutura(new DateOnly(2030, 1, 7), new TimeOnly(8, 0), ahora));
        Assert.True(Reglas.EsFechaHoraFutura(new DateOnly(2030, 1, 7), new TimeOnly(10, 0), ahora));
    }

    [Theory]
    [InlineData(EstadoPedido.PENDIENTE, EstadoPedido.LISTO_PARA_RECOGER, true)]
    [InlineData(EstadoPedido.ENTREGADO, EstadoPedido.CANCELADO, false)]
    [InlineData(EstadoPedido.CANCELADO, EstadoPedido.PENDIENTE, false)]
    public void TransicionPedidoValida_RespetaElFlujo(EstadoPedido desde, EstadoPedido hasta, bool esperado) =>
        Assert.Equal(esperado, Reglas.TransicionPedidoValida(desde, hasta));

    [Fact]
    public void Total_SumaSubtotalesConRedondeoACentavos() =>
        Assert.Equal(550.30m, Reglas.Total([350.10m, 200.20m]));

    [Fact]
    public void ProximasDosis_DevuelveLaUltimaDosisPorVacuna()
    {
        var aplicaciones = new[]
        {
            (VacunaId: 1, Fecha: new DateOnly(2025, 1, 1), Proxima: (DateOnly?)new DateOnly(2026, 1, 1)),
            (VacunaId: 1, Fecha: new DateOnly(2026, 1, 2), Proxima: (DateOnly?)new DateOnly(2027, 1, 2)),
            (VacunaId: 2, Fecha: new DateOnly(2026, 3, 1), Proxima: (DateOnly?)null),
        };
        var resultado = Reglas.ProximasDosis(aplicaciones, a => a.VacunaId, a => a.Fecha, a => a.Proxima);
        Assert.Single(resultado);
        Assert.Equal(new DateOnly(2027, 1, 2), resultado[0].Proxima);
    }
}
