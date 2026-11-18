using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PetCare.Application.Abstracciones;
using PetCare.Application.Common;
using PetCare.Domain;

namespace PetCare.Application.Servicios;

public record ResultadoRecordatorios(int Vacunas, int Citas);

public class RecordatoriosService(
    IVacunacionRepositorio vacunacion,
    ICitaRepositorio citas,
    INotificacionRepositorio notificaciones,
    IPushSender push,
    IUnitOfWork uow,
    IOptions<OpcionesApp> opciones,
    IReloj reloj,
    ILogger<RecordatoriosService> logger)
{
    public async Task<ResultadoRecordatorios> EjecutarAsync(CancellationToken ct)
    {
        var hoy = DateOnly.FromDateTime(reloj.Ahora);
        var vacunas = await vacunacion.PendientesRecordatorioAsync(hoy, hoy.AddDays(opciones.Value.RecordatorioDias), ct);
        foreach (var v in vacunas)
        {
            await NotificarAsync(v.Mascota.DuenoId,
                $"Vacuna próxima: {v.Mascota.Nombre}",
                $"{v.Vacuna.Nombre} de {v.Mascota.Nombre} corresponde el {v.ProximaDosis:yyyy-MM-dd}. Agenda tu cita.", ct);
            v.RecordatorioEnviado = true;
        }

        var citasManana = await citas.PendientesRecordatorioAsync(hoy.AddDays(1), ct);
        foreach (var c in citasManana)
        {
            await NotificarAsync(c.Mascota.DuenoId, "Cita mañana",
                $"{c.Mascota.Nombre} tiene cita de {c.Servicio.Nombre} el {c.Fecha:yyyy-MM-dd} a las {c.Hora:HH:mm}.", ct);
            c.RecordatorioEnviado = true;
        }

        await uow.GuardarAsync(ct);
        return new ResultadoRecordatorios(vacunas.Count, citasManana.Count);
    }

    private async Task NotificarAsync(int usuarioId, string titulo, string mensaje, CancellationToken ct)
    {
        notificaciones.Agregar(new Notificacion
        {
            UsuarioId = usuarioId,
            Titulo = titulo,
            Mensaje = mensaje,
            CreatedAt = reloj.Ahora,
        });
        await uow.GuardarAsync(ct);
        try
        {
            var tokens = await notificaciones.TokensDeUsuarioAsync(usuarioId, ct);
            await push.EnviarAsync(tokens, titulo, mensaje, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error enviando push al usuario {UsuarioId}", usuarioId);
        }
    }
}
