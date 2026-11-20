using PetCare.Application.Abstracciones;
using PetCare.Domain;

namespace PetCare.Application.Servicios;

public record ResultadoNotificacion(int Dispositivos, int Enviados);

public class Notificador(
    INotificacionRepositorio notificaciones,
    IPushSender push,
    IUnitOfWork uow,
    IReloj reloj)
{
    public async Task<ResultadoNotificacion> EnviarAsync(int usuarioId, string titulo, string mensaje, string tipo, CancellationToken ct)
    {
        notificaciones.Agregar(new Notificacion
        {
            UsuarioId = usuarioId,
            Titulo = titulo,
            Mensaje = mensaje,
            CreatedAt = reloj.Ahora,
        });
        await uow.GuardarAsync(ct);

        var tokens = await notificaciones.TokensDeUsuarioAsync(usuarioId, ct);
        if (tokens.Count == 0) return new ResultadoNotificacion(0, 0);

        var data = new Dictionary<string, string> { ["tipo"] = tipo };
        var resultado = await push.EnviarAsync(tokens, titulo, mensaje, data, ct);
        if (resultado is not null && resultado.Invalidos.Count > 0)
        {
            await notificaciones.EliminarDispositivosAsync(resultado.Invalidos, ct);
            await uow.GuardarAsync(ct);
        }
        return new ResultadoNotificacion(tokens.Count, resultado?.Enviados ?? 0);
    }
}
