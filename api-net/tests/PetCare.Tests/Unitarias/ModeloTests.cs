using Microsoft.EntityFrameworkCore;
using PetCare.Domain;
using PetCare.Infrastructure.Persistence;

namespace PetCare.Tests.Unitarias;

public class ModeloTests
{
    private static PetCareDbContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<PetCareDbContext>()
            .UseMySQL("Server=127.0.0.1;Port=1;Database=x;User ID=x;Password=x;")
            .Options;
        return new PetCareDbContext(opciones);
    }

    [Theory]
    [InlineData(typeof(VeterinarioHorario), nameof(VeterinarioHorario.HoraInicio))]
    [InlineData(typeof(VeterinarioHorario), nameof(VeterinarioHorario.HoraFin))]
    [InlineData(typeof(Cita), nameof(Cita.Hora))]
    public void Columnas_TIME_se_leen_como_TimeSpan_y_se_convierten_a_TimeOnly(Type entidad, string propiedad)
    {
        using var db = CrearContexto();
        var prop = db.Model.FindEntityType(entidad)!.FindProperty(propiedad)!;

        Assert.NotNull(prop.GetValueConverter());
        Assert.Equal(typeof(TimeSpan), prop.GetTypeMapping().Converter!.ProviderClrType);
    }
}
