using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

namespace PetCare.Api.Infraestructura;

public class SinPermisos : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _predeterminado = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext ctx, AuthorizationPolicy politica, PolicyAuthorizationResult resultado)
    {
        if (resultado.Forbidden && !resultado.Succeeded)
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            await ctx.Response.WriteAsJsonAsync(new ErrorRespuestaDto(false,
                new ErrorCuerpoDto("FORBIDDEN", "Tu rol no tiene permiso para esta acción", null)));
            return;
        }
        await _predeterminado.HandleAsync(next, ctx, politica, resultado);
    }
}
