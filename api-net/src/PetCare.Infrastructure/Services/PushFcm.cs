using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;
using PetCare.Application.Abstracciones;

namespace PetCare.Infrastructure.Services;

public class PushFcm(IOptions<FirebaseOpciones> opciones) : IPushSender
{
    private const string Canal = "petcare_recordatorios";
    private const string ColorMarca = "#1B7F5C";

    private static readonly MessagingErrorCode[] CodigosInvalidos =
    [
        MessagingErrorCode.Unregistered,
        MessagingErrorCode.InvalidArgument,
        MessagingErrorCode.SenderIdMismatch,
    ];

    private readonly FirebaseOpciones _opciones = opciones.Value;

    public bool Activo =>
        !string.IsNullOrWhiteSpace(_opciones.ServiceAccountPath) && File.Exists(_opciones.ServiceAccountPath);

    public async Task<ResultadoPush> EnviarAsync(IReadOnlyList<string> tokens, string titulo, string mensaje,
        IReadOnlyDictionary<string, string> data, CancellationToken ct)
    {
        if (tokens.Count == 0) return new ResultadoPush(0, []);
        if (!Activo)
        {
            Console.WriteLine($"[push-dev] {titulo}: {mensaje} ({tokens.Count} dispositivo(s))");
            return new ResultadoPush(0, []);
        }

        if (FirebaseApp.DefaultInstance is null)
        {
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(_opciones.ServiceAccountPath),
            });
        }

        var respuesta = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(new MulticastMessage
        {
            Tokens = tokens.ToList(),
            Notification = new Notification { Title = titulo, Body = mensaje },
            Data = data,
            Android = new AndroidConfig
            {
                Priority = Priority.High,
                Notification = new AndroidNotification { ChannelId = Canal, Color = ColorMarca },
            },
        }, ct);

        var invalidos = new List<string>();
        for (var i = 0; i < respuesta.Responses.Count; i++)
        {
            var codigo = respuesta.Responses[i].Exception?.MessagingErrorCode;
            if (codigo is not null && CodigosInvalidos.Contains(codigo.Value)) invalidos.Add(tokens[i]);
        }
        return new ResultadoPush(respuesta.SuccessCount, invalidos);
    }
}
