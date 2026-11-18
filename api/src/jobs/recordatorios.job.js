const cron = require('node-cron');

function programarRecordatorios({ ejecutarRecordatorios, config }) {
  if (!cron.validate(config.reminders.cron)) {
    throw new Error(`REMINDER_CRON inválido: ${config.reminders.cron}`);
  }
  return cron.schedule(
    config.reminders.cron,
    async () => {
      try {
        const resultado = await ejecutarRecordatorios();
        console.log(`[jobs] Recordatorios enviados: ${JSON.stringify(resultado)}`);
      } catch (err) {
        console.error('[jobs] Error ejecutando recordatorios:', err);
      }
    },
    { timezone: config.reminders.timezone }
  );
}

module.exports = { programarRecordatorios };
