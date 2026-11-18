const express = require('express');
const helmet = require('helmet');
const cors = require('cors');
const swaggerUi = require('swagger-ui-express');
const { notFound, errorHandler } = require('./middlewares/errorHandler');
const authRoutes = require('./routes/auth.routes');
const usuariosRoutes = require('./routes/usuarios.routes');
const catalogosRoutes = require('./routes/catalogos.routes');
const mascotasRoutes = require('./routes/mascotas.routes');
const citasRoutes = require('./routes/citas.routes');
const tiendaRoutes = require('./routes/tienda.routes');
const adminRoutes = require('./routes/admin.routes');
const openapi = require('../docs/openapi');

function crearApp({ config, security, useCases }) {
  const app = express();
  const contexto = { useCases, security };

  app.disable('x-powered-by');
  app.set('trust proxy', 1);
  app.use(helmet({ crossOriginResourcePolicy: { policy: 'cross-origin' } }));
  app.use(cors());
  app.use(express.json({ limit: '100kb' }));
  app.use('/uploads', express.static(config.uploadDir, { maxAge: '7d' }));

  app.get('/health', (_req, res) => res.json({ success: true, data: { status: 'ok' } }));
  app.get('/api-docs.json', (_req, res) => res.json(openapi));
  app.use('/api-docs', swaggerUi.serve, swaggerUi.setup(openapi));

  app.use('/api/auth', authRoutes({ auth: useCases.auth, security }));
  app.use('/api/mascotas', mascotasRoutes(contexto));
  app.use('/api/citas', citasRoutes(contexto));
  app.use('/api/admin', adminRoutes(contexto));
  app.use('/api', catalogosRoutes(contexto));
  app.use('/api', tiendaRoutes(contexto));
  app.use('/api', usuariosRoutes(contexto));

  app.use(notFound);
  app.use(errorHandler);
  return app;
}

module.exports = { crearApp };
