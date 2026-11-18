const fs = require('fs');
const path = require('path');
const { query, pool } = require('../src/infrastructure/database/db');

const CARPETA = path.resolve(__dirname, '../migrations');

const dividirSentencias = (sql) =>
  sql
    .split(/;\s*$/m)
    .map((s) => s.trim())
    .filter(Boolean);

async function migrar() {
  await query(`CREATE TABLE IF NOT EXISTS schema_migrations (
    version VARCHAR(100) PRIMARY KEY,
    applied_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
  ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4`);

  const aplicadas = new Set(
    (await query('SELECT version FROM schema_migrations')).map((r) => r.version)
  );
  const archivos = fs
    .readdirSync(CARPETA)
    .filter((f) => f.endsWith('.sql'))
    .sort();

  for (const archivo of archivos) {
    if (aplicadas.has(archivo)) {
      console.log(`- ${archivo} (ya aplicada)`);
      continue;
    }
    const sql = fs.readFileSync(path.join(CARPETA, archivo), 'utf8');
    for (const sentencia of dividirSentencias(sql)) {
      await query(sentencia);
    }
    await query('INSERT INTO schema_migrations (version) VALUES (?)', [archivo]);
    console.log(`+ ${archivo} aplicada`);
  }
  console.log('Migraciones completadas.');
}

migrar()
  .catch((err) => {
    console.error('Error en migraciones:', err.message);
    process.exitCode = 1;
  })
  .finally(() => pool.end());
