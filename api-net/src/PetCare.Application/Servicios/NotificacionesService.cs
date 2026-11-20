using PetCare.Application.Abstracciones;
using PetCare.Application.Common;
using PetCare.Application.Contratos;
using PetCare.Domain;

namespace PetCare.Application.Servicios;

public class NotificacionesService(
    INotificacionRepositorio notificaciones,
    IPushSender push,
    IUnitOfWork uow,
    IReloj reloj,
    Mapeador mapeador)
{
    private readonly Notificador _notificador = new(notificaciones, push, uow, reloj);

    public async Task<IReadOnlyList<NotificacionDto>> ListarAsync(int usuarioId, CancellationToken ct) =>
        (await notificaciones.DeUsuarioAsync(usuarioId, ct)).Select(mapeador.Notificacion).ToList();

    public async Task<MensajeDto> MarcarLeidaAsync(int usuarioId, int id, CancellationToken ct)
    {
        var notificacion = await notificaciones.DeUsuarioPorIdAsync(id, usuarioId, ct)
            ?? throw new NoEncontradoException("Notificación no encontrada");
        notificacion.Leida = true;
        await uow.GuardarAsync(ct);
        return new MensajeDto("Notificación marcada como leída");
    }

    public async Task<MensajeDto> RegistrarDispositivoAsync(int usuarioId, string fcmToken, CancellationToken ct)
    {
        await notificaciones.GuardarDispositivoAsync(usuarioId, fcmToken, ct);
        await uow.GuardarAsync(ct);
        return new MensajeDto("Dispositivo registrado");
    }

    public async Task<PruebaPushDto> ProbarAsync(int usuarioId, CancellationToken ct)
    {
        var resultado = await _notificador.EnviarAsync(usuarioId, "Notificación de prueba",
            "Las notificaciones de PetCare HN están funcionando en este dispositivo.", "PRUEBA", ct);
        var mensaje = resultado.Dispositivos > 0
            ? "Notificación enviada a tus dispositivos."
            : "Notificación guardada. No hay dispositivos registrados para recibir push.";
        return new PruebaPushDto(mensaje, resultado.Dispositivos, resultado.Enviados);
    }
}
