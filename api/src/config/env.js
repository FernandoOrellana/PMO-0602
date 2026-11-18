const path = require('path');
require('dotenv').config({ path: path.resolve(__dirname, '../../.env'), quiet: true });

const required = (name) => {
  const value = process.env[name];
  if (!value) throw new Error(`Falta la variable de entorno ${name}`);
  return value;
};

const port = Number(process.env.PORT || 3000);

module.exports = {
  env: process.env.NODE_ENV || 'development',
  port,
  publicBaseUrl: (process.env.PUBLIC_BASE_URL || `http://localhost:${port}`).replace(/\/$/, ''),
  db: {
    host: required('DB_HOST'),
    port: Number(required('DB_PORT')),
    user: required('DB_USER'),
    password: process.env.DB_PASSWORD ?? '',
    database: required('DB_NAME'),
  },
  jwt: {
    accessSecret: required('JWT_ACCESS_SECRET'),
    accessMinutes: Number(process.env.JWT_ACCESS_MINUTES || 30),
  },
  refreshTokenDays: Number(process.env.REFRESH_TOKEN_DAYS || 7),
  codigoExpiraMinutos: Number(process.env.CODIGO_EXPIRA_MINUTOS || 15),
  smtp: {
    host: process.env.SMTP_HOST || '',
    port: Number(process.env.SMTP_PORT || 587),
    secure: process.env.SMTP_SECURE === 'true',
    user: process.env.SMTP_USER || '',
    pass: process.env.SMTP_PASS || '',
    from: process.env.SMTP_FROM || 'PetCare HN <no-reply@huellitas.hn>',
  },
  firebaseServiceAccountPath: process.env.FIREBASE_SERVICE_ACCOUNT_PATH || '',
  uploadDir: path.resolve(__dirname, '../..', process.env.UPLOAD_DIR || './uploads'),
  reminders: {
    cron: process.env.REMINDER_CRON || '0 8 * * *',
    days: Number(process.env.REMINDER_DAYS || 7),
    timezone: process.env.REMINDER_TZ || 'America/Tegucigalpa',
  },
  seed: {
    adminEmail: process.env.SEED_ADMIN_EMAIL || 'admin@huellitas.hn',
    adminPassword: process.env.SEED_ADMIN_PASSWORD || '',
    demoPassword: process.env.SEED_DEMO_PASSWORD || 'Demo12345',
  },
};
