using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PetCare.Application.Common;

namespace PetCare.Api.Infraestructura;

public abstract class ControladorBase : ControllerBase
{
    private const long TamanoMaximoImagen = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> Extensiones = new()
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
    };

    protected Actor Actor =>
        new(int.Parse(User.FindFirst("sub")!.Value), User.FindFirst("rol")!.Value);

    protected IActionResult Respuesta<T>(T data, int status = StatusCodes.Status200OK) =>
        StatusCode(status, new RespuestaDto<T>(true, data));

    protected IActionResult Lista<T>(Pagina<T> pagina) =>
        Ok(new ListaDto<T>(true, pagina.Items, pagina.Meta));

    protected static async Task ValidarAsync<T>(IValidator<T> validador, T dto)
    {
        var resultado = await validador.ValidateAsync(dto);
        if (!resultado.IsValid)
        {
            throw new ValidacionException("Datos inválidos",
                resultado.Errors.Select(e => new ErrorDetalle(MinusculaInicial(e.PropertyName), e.ErrorMessage)).ToList());
        }
    }

    private static string MinusculaInicial(string nombre) =>
        string.IsNullOrEmpty(nombre) ? nombre : char.ToLowerInvariant(nombre[0]) + nombre[1..];

    protected static (Stream Contenido, string Extension) ImagenDe(IFormFile? archivo)
    {
        if (archivo is null || archivo.Length == 0 || archivo.Length > TamanoMaximoImagen
            || !Extensiones.TryGetValue(archivo.ContentType, out var extension))
        {
            throw new ValidacionException("Debes enviar una imagen JPG, PNG o WEBP de máximo 5 MB");
        }
        return (archivo.OpenReadStream(), extension);
    }

    protected static (int Page, int Limit) ValidarPaginacion(int page, int limit)
    {
        if (page < 1) throw new ValidacionException("Datos inválidos", [new ErrorDetalle("page", "Debe ser mayor o igual a 1")]);
        if (limit < 1 || limit > 100) throw new ValidacionException("Datos inválidos", [new ErrorDetalle("limit", "Debe estar entre 1 y 100")]);
        return (page, limit);
    }
}
