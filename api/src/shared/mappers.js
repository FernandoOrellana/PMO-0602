const config = require('../config/env');

const toUsuario = (u) => ({
  id: u.id,
  nombre: u.nombre,
  email: u.email,
  telefono: u.telefono ?? null,
  rol: u.rol,
});

const urlPublica = (ruta) => (ruta ? `${config.publicBaseUrl}/uploads/${ruta}` : null);

const paginar = ({ page, limit }) => ({ offset: (page - 1) * limit, limit });

const meta = ({ page, limit, total }) => ({
  page,
  limit,
  total,
  totalPages: Math.max(1, Math.ceil(total / limit)),
});

module.exports = { toUsuario, urlPublica, paginar, meta };
