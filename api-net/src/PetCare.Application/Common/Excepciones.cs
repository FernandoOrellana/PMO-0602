namespace PetCare.Application.Common;

public record ErrorDetalle(string Campo, string Mensaje);

public abstract class AppException(string mensaje, int status, string codigo, IReadOnlyList<ErrorDetalle>? detalles = null)
    : Exception(mensaje)
{
    public int Status { get; } = status;
    public string Codigo { get; } = codigo;
    public IReadOnlyList<ErrorDetalle>? Detalles { get; } = detalles;
}

public sealed class ValidacionException(string mensaje = "Datos inválidos", IReadOnlyList<ErrorDetalle>? detalles = null)
    : AppException(mensaje, 400, "VALIDATION_ERROR", detalles);

public sealed class NoAutenticadoException(string mensaje = "Autenticación requerida")
    : AppException(mensaje, 401, "UNAUTHORIZED");

public sealed class CredencialesInvalidasException()
    : AppException("Correo o contraseña incorrectos", 401, "INVALID_CREDENTIALS");

public sealed class ProhibidoException(string mensaje = "No tienes permiso para realizar esta acción")
    : AppException(mensaje, 403, "FORBIDDEN");

public sealed class CorreoNoVerificadoException()
    : AppException("Debes verificar tu correo antes de iniciar sesión", 403, "EMAIL_NOT_VERIFIED");

public sealed class NoEncontradoException(string mensaje = "Recurso no encontrado")
    : AppException(mensaje, 404, "NOT_FOUND");

public sealed class ConflictoException(string mensaje = "Conflicto con el estado actual del recurso")
    : AppException(mensaje, 409, "CONFLICT");
