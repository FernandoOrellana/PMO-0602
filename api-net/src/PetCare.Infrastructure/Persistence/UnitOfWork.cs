using Microsoft.EntityFrameworkCore;
using PetCare.Application.Abstracciones;
using PetCare.Application.Common;

namespace PetCare.Infrastructure.Persistence;

public class UnitOfWork(PetCareDbContext db) : IUnitOfWork
{
    public async Task GuardarAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("Duplicate entry") == true)
        {
            throw new ConflictoException("Ya existe un registro con esos datos");
        }
    }
}
