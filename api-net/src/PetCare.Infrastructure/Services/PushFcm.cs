using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;
using PetCare.Application.Abstracciones;

namespace PetCare.Infrastructure.Services;

public class PushFcm(IOptions<FirebaseOpciones> opciones) : IPushSender
{
    private readonly FirebaseOpciones _opciones = opciones.Value;

    public async Task EnviarAsync(IReadOnlyList<string> tokens, string titulo, string mensaje, CancellationToken ct)
    {
        if (tokens.Count == 0) return;
        if (string.IsNullOrWhiteSpace(_opciones.ServiceAccountPath) || !File.Exists(_opciones.ServiceAccountPath))
        {
            Console.WriteLine($"[push-dev] {titulo}: {mensaje} ({tokens.Count} dispositivo(s))");
            return;
        }

        if (FirebaseApp.DefaultInstance is null)
        {
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(_opciones.ServiceAccountPath),
            });
        }

        await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(new MulticastMessage
        {
            Tokens = tokens.ToList(),
            Notification = new Notification { Title = titulo, Body = mensaje },
        }, ct);
    }
}
