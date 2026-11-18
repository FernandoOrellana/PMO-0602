using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PetCare.Application.Abstracciones;
using PetCare.Application.Servicios;

namespace PetCare.Infrastructure.Jobs;

public class RecordatoriosHostedService(
    IServiceScopeFactory scopes,
    IReloj reloj,
    IOptions<RecordatoriosOpciones> opciones,
    ILogger<RecordatoriosHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var opcion = opciones.Value;
        if (!opcion.Habilitado) return;

        var hora = TimeOnly.ParseExact(opcion.Hora, "HH:mm");
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(EsperaHasta(hora), ct);
            try
            {
                using var scope = scopes.CreateScope();
                var resultado = await scope.ServiceProvider.GetRequiredService<RecordatoriosService>().EjecutarAsync(ct);
                logger.LogInformation("Recordatorios enviados: {Vacunas} vacunas, {Citas} citas", resultado.Vacunas, resultado.Citas);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error ejecutando recordatorios");
            }
            await Task.Delay(TimeSpan.FromSeconds(60), ct);
        }
    }

    private TimeSpan EsperaHasta(TimeOnly hora)
    {
        var ahora = reloj.Ahora;
        var proxima = ahora.Date.Add(hora.ToTimeSpan());
        if (proxima <= ahora) proxima = proxima.AddDays(1);
        return proxima - ahora;
    }
}
