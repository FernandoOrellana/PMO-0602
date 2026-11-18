using PetCare.Application.Abstracciones;
using PetCare.Application.Common;
using PetCare.Application.Contratos;
using PetCare.Domain;

namespace PetCare.Application.Servicios;

public class NotificacionesService(
    INotificacionRepositorio notificaciones,
    IUnitOfWork uow,
    Mapeador mapeador)
{
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
}
