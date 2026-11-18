using Microsoft.Extensions.Options;
using PetCare.Application.Abstracciones;
using PetCare.Application.Common;

namespace PetCare.Infrastructure.Services;

public class RelojLocal(IOptions<OpcionesApp> opciones) : IReloj
{
    private readonly TimeZoneInfo _zona = TimeZoneInfo.FindSystemTimeZoneById(opciones.Value.ZonaHoraria);

    public DateTime Ahora => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _zona);
}
