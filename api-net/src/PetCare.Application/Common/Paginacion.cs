namespace PetCare.Application.Common;

public record Meta(int Page, int Limit, int Total, int TotalPages);

public record Pagina<T>(IReadOnlyList<T> Items, Meta Meta);

public static class Paginacion
{
    public static Meta Crear(int page, int limit, int total) =>
        new(page, limit, total, Math.Max(1, (int)Math.Ceiling(total / (double)limit)));
}

public record Actor(int Id, string Rol);
