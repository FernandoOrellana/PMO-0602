const { config, security, useCases } = require('./container');
const { pool, query } = require('./infrastructure/database/db');
const { crearApp } = require('./presentation/http/app');
const { programarRecordatorios } = require('./jobs/recordatorios.job');

const app = crearApp({ config, security, useCases });

async function iniciar() {
  try {
    await query('SELECT 1');
  } catch (err) {
    console.error(`No se pudo conectar a MySQL (${config.db.host}:${config.db.port}): ${err.message}`);
    process.exit(1);
  }

  programarRecordatorios({ ejecutarRecordatorios: useCases.recordatorios, config });

  const servidor = app.listen(config.port, () => {
    console.log(`API PetCare HN escuchando en http://localhost:${config.port}`);
    console.log(`Swagger: ${config.publicBaseUrl}/api-docs`);
  });

  const apagar = () => {
    servidor.close(() => pool.end().then(() => process.exit(0)));
  };
  process.on('SIGINT', apagar);
  process.on('SIGTERM', apagar);
}

iniciar();
