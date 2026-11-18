using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using PetCare.Application.Abstracciones;
using PetCare.Application.Common;

namespace PetCare.Infrastructure.Services;

public class CorreoSmtp(IOptions<SmtpOpciones> opciones, IOptions<OpcionesApp> app) : INotificadorCorreo
{
    private readonly SmtpOpciones _smtp = opciones.Value;
    private readonly OpcionesApp _app = app.Value;

    public Task EnviarCodigoVerificacionAsync(string email, string codigo, CancellationToken ct) =>
        EnviarAsync(email, "Código de verificación - Veterinaria Huellitas",
            $"Tu código de verificación es: {codigo}\nExpira en {_app.CodigoExpiraMinutos} minutos.", ct);

    public Task EnviarClaveTemporalAsync(string email, string clave, CancellationToken ct) =>
        EnviarAsync(email, "Clave temporal - Veterinaria Huellitas",
            $"Tu clave temporal es: {clave}\nExpira en {_app.CodigoExpiraMinutos} minutos. Úsala para restablecer tu contraseña.", ct);

    private async Task EnviarAsync(string destino, string asunto, string texto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_smtp.Host))
        {
            Console.WriteLine($"[correo-dev] Para: {destino} | {asunto}\n{texto}");
            return;
        }

        var mensaje = new MimeMessage();
        mensaje.From.Add(MailboxAddress.Parse(_smtp.From));
        mensaje.To.Add(MailboxAddress.Parse(destino));
        mensaje.Subject = asunto;
        mensaje.Body = new TextPart("plain") { Text = texto };

        using var cliente = new SmtpClient();
        var seguridad = _smtp.Secure ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
        await cliente.ConnectAsync(_smtp.Host, _smtp.Port, seguridad, ct);
        if (!string.IsNullOrWhiteSpace(_smtp.User))
            await cliente.AuthenticateAsync(_smtp.User, _smtp.Password, ct);
        await cliente.SendAsync(mensaje, ct);
        await cliente.DisconnectAsync(true, ct);
    }
}
