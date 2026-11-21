using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MySql.Data.MySqlClient;
using PetCare.Application.Abstracciones;
using PetCare.Application.Common;
using PetCare.Domain;

namespace PetCare.Infrastructure.Persistence;

public class CobroRepositorio(PetCareDbContext db) : ICobroRepositorio
{
    public async Task<T> EnTransaccionAsync<T>(bool esPedido, int origenId, Func<Task<T>> operacion, CancellationToken ct)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            await using var comando = db.Database.GetDbConnection().CreateCommand();
            comando.Transaction = transaccion.GetDbTransaction();
            comando.CommandText = esPedido ? "SELECT id FROM pedidos WHERE id = @id FOR UPDATE" : "SELECT id FROM citas WHERE id = @id FOR UPDATE";
            var parametro = comando.CreateParameter();
            parametro.ParameterName = "@id";
            parametro.Value = origenId;
            comando.Parameters.Add(parametro);
            if (await comando.ExecuteScalarAsync(ct) is null)
                throw new NoEncontradoException(esPedido ? "Pedido no encontrado" : "Cita no encontrada");
            var resultado = await operacion();
            await transaccion.CommitAsync(ct);
            return resultado;
        }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException { Number: 1062 })
        {
            await transaccion.RollbackAsync(ct);
            throw new ConflictoException("Ya existe un cobro para esta operación o la clave está en uso");
        }
    }

    public Task<Cobro?> PorClaveAsync(string clave, CancellationToken ct) =>
        db.Cobros.Include(c => c.Items).FirstOrDefaultAsync(c => c.ClaveIdempotencia == clave, ct);

    public Task<Cobro?> PorOrigenAsync(bool esPedido, int origenId, CancellationToken ct) =>
        db.Cobros.Include(c => c.Items).FirstOrDefaultAsync(c => esPedido ? c.PedidoId == origenId : c.CitaId == origenId, ct);

    public Task<Cobro?> PorIdAsync(int id, CancellationToken ct) =>
        db.Cobros.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == id, ct);

    public void Agregar(Cobro cobro) => db.Cobros.Add(cobro);
    public async Task GuardarAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);
}
