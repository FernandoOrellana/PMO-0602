using Microsoft.Extensions.Options;
using PetCare.Application.Abstracciones;
using PetCare.Application.Common;

namespace PetCare.Infrastructure.Services;

public class AlmacenLocal(IOptions<AlmacenOpciones> opciones, IOptions<OpcionesApp> app) : IAlmacenArchivos
{
    private readonly string _raiz = Path.GetFullPath(opciones.Value.Carpeta);
    private readonly string _publicBase = app.Value.PublicBaseUrl.TrimEnd('/');

    public async Task<string> GuardarAsync(string carpeta, Stream contenido, string extension, CancellationToken ct)
    {
        var directorio = Path.Combine(_raiz, carpeta);
        Directory.CreateDirectory(directorio);
        var nombre = $"{Guid.NewGuid()}{extension}";
        await using var destino = File.Create(Path.Combine(directorio, nombre));
        await contenido.CopyToAsync(destino, ct);
        return $"{carpeta}/{nombre}";
    }

    public Task EliminarAsync(string? ruta, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ruta)) return Task.CompletedTask;
        var absoluta = Path.GetFullPath(Path.Combine(_raiz, ruta));
        if (absoluta.StartsWith(_raiz, StringComparison.Ordinal) && File.Exists(absoluta)) File.Delete(absoluta);
        return Task.CompletedTask;
    }

    public string? UrlPublica(string? ruta) => string.IsNullOrWhiteSpace(ruta) ? null : $"{_publicBase}/uploads/{ruta}";
}
