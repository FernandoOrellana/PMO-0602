using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace PetCare.Infrastructure.Persistence;

public static class Migrador
{
    public static async Task MigrarAsync(PetCareDbContext db, CancellationToken ct = default)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS schema_migrations (
              version VARCHAR(100) PRIMARY KEY,
              applied_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """, ct);

        var aplicadas = await db.Database.SqlQueryRaw<string>("SELECT version AS Value FROM schema_migrations").ToListAsync(ct);
        foreach (var (nombre, sql) in Scripts())
        {
            if (aplicadas.Contains(nombre))
            {
                Console.WriteLine($"- {nombre} (ya aplicada)");
                continue;
            }
            foreach (var sentencia in Dividir(sql))
                await db.Database.ExecuteSqlRawAsync(sentencia, ct);
            await db.Database.ExecuteSqlRawAsync("INSERT INTO schema_migrations (version) VALUES ({0})", [nombre], ct);
            Console.WriteLine($"+ {nombre} aplicada");
        }
        Console.WriteLine("Migraciones completadas.");
    }

    private static IEnumerable<(string Nombre, string Sql)> Scripts()
    {
        var asm = Assembly.GetExecutingAssembly();
        foreach (var recurso in asm.GetManifestResourceNames().Where(r => r.EndsWith(".sql")).OrderBy(r => r))
        {
            using var stream = asm.GetManifestResourceStream(recurso)!;
            using var lector = new StreamReader(stream);
            var nombre = recurso[(recurso.LastIndexOf('.', recurso.Length - 5) + 1)..];
            yield return (nombre, lector.ReadToEnd());
        }
    }

    private static IEnumerable<string> Dividir(string sql) =>
        Regex.Split(sql, @";\s*$", RegexOptions.Multiline).Select(s => s.Trim()).Where(s => s.Length > 0);
}
