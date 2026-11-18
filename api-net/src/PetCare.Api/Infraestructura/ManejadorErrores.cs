using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using PetCare.Application.Common;

namespace PetCare.Api.Infraestructura;

public class ManejadorErrores(ILogger<ManejadorErrores> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var (status, codigo, mensaje, detalles) = ex switch
        {
            AppException app => (app.Status, app.Codigo, app.Message, app.Detalles),
            BadHttpRequestException => (400, "VALIDATION_ERROR", "El cuerpo de la petición no es válido", null),
            _ => (500, "INTERNAL_ERROR", "Error interno del servidor", null),
        };

        if (status == 500) logger.LogError(ex, "Error no controlado");

        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(new ErrorRespuestaDto(false, new ErrorCuerpoDto(codigo, mensaje, detalles)), ct);
        return true;
    }
}
