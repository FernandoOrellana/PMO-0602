using System.Text.Json.Serialization;
using PetCare.Application.Common;

namespace PetCare.Api.Infraestructura;

public record RespuestaDto<T>(bool Success, T Data);

public record ListaDto<T>(bool Success, IReadOnlyList<T> Data, Meta Meta);

public record ErrorRespuestaDto(bool Success, ErrorCuerpoDto Error);

public record ErrorCuerpoDto(
    string Code,
    string Message,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<ErrorDetalle>? Details);
