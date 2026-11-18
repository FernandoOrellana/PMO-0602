using PetCare.Domain;

namespace PetCare.Application.Abstracciones;

public interface ISeguridad
{
    string HashPassword(string password);
    bool VerificarPassword(string password, string hash);
    string CrearAccessToken(Usuario usuario);
    int AccessTokenSegundos { get; }
    string NuevoRefreshToken();
    string Sha256(string valor);
    string NuevoCodigo();
    string NuevaClaveTemporal();
}

public interface IReloj
{
    DateTime Ahora { get; }
}

public interface IUnitOfWork
{
    Task GuardarAsync(CancellationToken ct);
}

public interface INotificadorCorreo
{
    Task EnviarCodigoVerificacionAsync(string email, string codigo, CancellationToken ct);
    Task EnviarClaveTemporalAsync(string email, string clave, CancellationToken ct);
}

public interface IPushSender
{
    Task EnviarAsync(IReadOnlyList<string> tokens, string titulo, string mensaje, CancellationToken ct);
}

public interface IAlmacenArchivos
{
    Task<string> GuardarAsync(string carpeta, Stream contenido, string extension, CancellationToken ct);
    Task EliminarAsync(string? ruta, CancellationToken ct);
    string? UrlPublica(string? ruta);
}
