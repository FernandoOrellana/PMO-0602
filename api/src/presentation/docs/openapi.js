const config = require('../../config/env');

const ref = (name) => ({ $ref: `#/components/schemas/${name}` });
const json = (schema) => ({ 'application/json': { schema } });
const dataOf = (schema) => ({ type: 'object', properties: { success: { type: 'boolean', example: true }, data: schema } });
const listOf = (schema) => ({
  type: 'object',
  properties: {
    success: { type: 'boolean', example: true },
    data: { type: 'array', items: schema },
    meta: ref('Meta'),
  },
});
const idParam = { name: 'id', in: 'path', required: true, schema: { type: 'integer' } };
const pageParams = [
  { name: 'page', in: 'query', schema: { type: 'integer', default: 1 } },
  { name: 'limit', in: 'query', schema: { type: 'integer', default: 20, maximum: 100 } },
];
const SEC = [{ bearerAuth: [] }];

const resp = (desc, schema) => ({ description: desc, ...(schema ? { content: json(schema) } : {}) });
const ERR = (desc) => resp(desc, ref('Error'));

const op = ({ tag, summary, roles, auth = true, params = [], body, status = '200', ok, lista = false, extra = {} }) => ({
  tags: [tag],
  summary: roles ? `${summary} (${roles})` : summary,
  ...(auth ? { security: SEC } : { security: [] }),
  ...(params.length ? { parameters: params } : {}),
  ...(body ? { requestBody: { required: true, content: json(body) } } : {}),
  responses: {
    [status]: resp('OK', lista ? listOf(ok) : dataOf(ok)),
    400: ERR('Datos inválidos'),
    ...(auth ? { 401: ERR('Sin token o token vencido'), 403: ERR('Sin permiso para este rol') } : {}),
    404: ERR('No encontrado'),
  },
  ...extra,
});

const str = (extra = {}) => ({ type: 'string', ...extra });
const int = (extra = {}) => ({ type: 'integer', ...extra });
const num = (extra = {}) => ({ type: 'number', ...extra });

const paths = {
  '/api/auth/register': { post: op({ tag: 'Auth', summary: 'Registrar dueño (envía código al correo)', auth: false, status: '201', body: { type: 'object', required: ['nombre', 'email', 'password'], properties: { nombre: str(), email: str({ format: 'email' }), password: str({ minLength: 8 }), telefono: str() } }, ok: { type: 'object', properties: { message: str() } } }) },
  '/api/auth/verify-email': { post: op({ tag: 'Auth', summary: 'Verificar correo con código de 6 dígitos', auth: false, body: { type: 'object', required: ['email', 'codigo'], properties: { email: str(), codigo: str({ pattern: '^\\d{6}$' }) } }, ok: { type: 'object', properties: { message: str() } } }) },
  '/api/auth/resend-code': { post: op({ tag: 'Auth', summary: 'Reenviar código de verificación', auth: false, body: { type: 'object', required: ['email'], properties: { email: str() } }, ok: { type: 'object', properties: { message: str() } } }) },
  '/api/auth/login': { post: op({ tag: 'Auth', summary: 'Iniciar sesión', auth: false, body: { type: 'object', required: ['email', 'password'], properties: { email: str(), password: str() } }, ok: ref('Sesion') }) },
  '/api/auth/refresh': { post: op({ tag: 'Auth', summary: 'Renovar tokens (rota el refresh token)', auth: false, body: { type: 'object', required: ['refreshToken'], properties: { refreshToken: str() } }, ok: ref('Sesion') }) },
  '/api/auth/forgot-password': { post: op({ tag: 'Auth', summary: 'Enviar clave temporal al correo', auth: false, body: { type: 'object', required: ['email'], properties: { email: str() } }, ok: { type: 'object', properties: { message: str() } } }) },
  '/api/auth/reset-password': { post: op({ tag: 'Auth', summary: 'Restablecer contraseña con clave temporal', auth: false, body: { type: 'object', required: ['email', 'claveTemporal', 'nuevaPassword'], properties: { email: str(), claveTemporal: str(), nuevaPassword: str({ minLength: 8 }) } }, ok: { type: 'object', properties: { message: str() } } }) },
  '/api/auth/logout': { post: op({ tag: 'Auth', summary: 'Cerrar sesión (revoca el refresh token)', auth: false, body: { type: 'object', required: ['refreshToken'], properties: { refreshToken: str() } }, ok: { type: 'object', properties: { message: str() } } }) },
  '/api/auth/me': { get: op({ tag: 'Auth', summary: 'Datos del usuario autenticado', ok: ref('Usuario') }) },

  '/api/usuarios/me/dispositivos': { post: op({ tag: 'Usuarios', summary: 'Registrar token FCM del dispositivo', body: { type: 'object', required: ['fcmToken'], properties: { fcmToken: str() } }, ok: { type: 'object', properties: { message: str() } } }) },
  '/api/notificaciones': { get: op({ tag: 'Usuarios', summary: 'Listar notificaciones', lista: false, ok: { type: 'array', items: ref('Notificacion') } }) },
  '/api/notificaciones/{id}/leer': { patch: op({ tag: 'Usuarios', summary: 'Marcar notificación como leída', params: [idParam], ok: { type: 'object', properties: { message: str() } } }) },

  '/api/especies': { get: op({ tag: 'Catálogos', summary: 'Listar especies', ok: { type: 'array', items: ref('Especie') } }) },
  '/api/vacunas': { get: op({ tag: 'Catálogos', summary: 'Listar vacunas', ok: { type: 'array', items: ref('Vacuna') } }) },
  '/api/servicios': { get: op({ tag: 'Catálogos', summary: 'Listar servicios activos', ok: { type: 'array', items: ref('Servicio') } }) },
  '/api/veterinarios': { get: op({ tag: 'Catálogos', summary: 'Listar veterinarios con horarios', ok: { type: 'array', items: ref('Veterinario') } }) },
  '/api/veterinarios/{id}/disponibilidad': { get: op({ tag: 'Catálogos', summary: 'Horas disponibles de un veterinario', params: [idParam, { name: 'fecha', in: 'query', required: true, schema: str({ format: 'date' }) }, { name: 'servicioId', in: 'query', schema: int() }], ok: { type: 'object', properties: { fecha: str(), veterinario: ref('VeterinarioResumen'), horas: { type: 'array', items: str({ example: '08:30' }) } } } }) },

  '/api/mascotas': {
    get: op({ tag: 'Mascotas', summary: 'Listar mascotas (dueño: solo las suyas)', params: pageParams, lista: true, ok: ref('Mascota') }),
    post: op({ tag: 'Mascotas', summary: 'Registrar mascota', roles: 'DUENO', status: '201', body: ref('MascotaInput'), ok: ref('Mascota') }),
  },
  '/api/mascotas/{id}': {
    get: op({ tag: 'Mascotas', summary: 'Obtener mascota', params: [idParam], ok: ref('Mascota') }),
    put: op({ tag: 'Mascotas', summary: 'Actualizar mascota', roles: 'DUENO', params: [idParam], body: ref('MascotaInput'), ok: ref('Mascota') }),
    delete: op({ tag: 'Mascotas', summary: 'Eliminar mascota (lógico)', roles: 'DUENO', params: [idParam], ok: { type: 'object', properties: { message: str() } } }),
  },
  '/api/mascotas/{id}/foto': { post: op({ tag: 'Mascotas', summary: 'Subir foto de la mascota', roles: 'DUENO', params: [idParam], extra: { requestBody: { required: true, content: { 'multipart/form-data': { schema: { type: 'object', properties: { foto: { type: 'string', format: 'binary' } } } } } } }, ok: ref('Mascota') }) },
  '/api/mascotas/{id}/carne': { get: op({ tag: 'Mascotas', summary: 'Carné de vacunación digital', params: [idParam], ok: { type: 'object', properties: { mascota: ref('Mascota'), vacunas: { type: 'array', items: ref('AplicacionVacuna') }, proximasDosis: { type: 'array', items: ref('ProximaDosis') } } } }) },
  '/api/mascotas/{id}/historial': { get: op({ tag: 'Mascotas', summary: 'Historial clínico', params: [idParam], ok: { type: 'object', properties: { mascota: ref('Mascota'), consultas: { type: 'array', items: ref('Consulta') }, vacunas: { type: 'array', items: ref('AplicacionVacuna') }, citas: { type: 'array', items: ref('Cita') } } } }) },
  '/api/mascotas/{id}/vacunas': { post: op({ tag: 'Mascotas', summary: 'Registrar vacuna aplicada', roles: 'VETERINARIO', status: '201', params: [idParam], body: { type: 'object', required: ['vacunaId', 'fechaAplicacion'], properties: { vacunaId: int(), fechaAplicacion: str({ format: 'date' }), lote: str(), proximaDosis: str({ format: 'date' }), citaId: int() } }, ok: ref('AplicacionVacuna') }) },

  '/api/citas': {
    get: op({ tag: 'Citas', summary: 'Listar citas (según rol)', params: [...pageParams, { name: 'fecha', in: 'query', schema: str({ format: 'date' }) }, { name: 'estado', in: 'query', schema: str({ enum: ['PENDIENTE', 'CONFIRMADA', 'ATENDIDA', 'CANCELADA'] }) }], lista: true, ok: ref('Cita') }),
    post: op({ tag: 'Citas', summary: 'Agendar cita', roles: 'DUENO', status: '201', body: { type: 'object', required: ['mascotaId', 'servicioId', 'veterinarioId', 'fecha', 'hora'], properties: { mascotaId: int(), servicioId: int(), veterinarioId: int(), fecha: str({ format: 'date' }), hora: str({ example: '09:00' }) } }, ok: ref('Cita') }),
  },
  '/api/citas/{id}': { get: op({ tag: 'Citas', summary: 'Detalle de cita', params: [idParam], ok: ref('Cita') }) },
  '/api/citas/{id}/cancelar': { patch: op({ tag: 'Citas', summary: 'Cancelar cita', roles: 'DUENO', params: [idParam], ok: ref('Cita') }) },
  '/api/citas/{id}/consulta': { post: op({ tag: 'Citas', summary: 'Registrar consulta (marca la cita como ATENDIDA)', roles: 'VETERINARIO', status: '201', params: [idParam], body: { type: 'object', required: ['diagnostico'], properties: { diagnostico: str(), tratamiento: str(), observaciones: str(), peso: num() } }, ok: ref('Consulta') }) },

  '/api/productos': { get: op({ tag: 'Tienda', summary: 'Catálogo de productos activos', params: [...pageParams, { name: 'categoria', in: 'query', schema: str() }], lista: true, ok: ref('Producto') }) },
  '/api/productos/{id}': { get: op({ tag: 'Tienda', summary: 'Detalle de producto', params: [idParam], ok: ref('Producto') }) },
  '/api/pedidos': {
    get: op({ tag: 'Tienda', summary: 'Listar pedidos (dueño: los suyos; admin: todos)', roles: 'DUENO, ADMINISTRADOR', params: pageParams, lista: true, ok: ref('Pedido') }),
    post: op({ tag: 'Tienda', summary: 'Crear pedido para recoger (pago en mostrador)', roles: 'DUENO', status: '201', body: { type: 'object', required: ['items'], properties: { items: { type: 'array', items: { type: 'object', required: ['productoId', 'cantidad'], properties: { productoId: int(), cantidad: int({ minimum: 1 }) } } } } }, ok: ref('Pedido') }),
  },
  '/api/pedidos/{id}': { get: op({ tag: 'Tienda', summary: 'Detalle de pedido', roles: 'DUENO, ADMINISTRADOR', params: [idParam], ok: ref('Pedido') }) },
  '/api/pedidos/{id}/estado': { patch: op({ tag: 'Tienda', summary: 'Cambiar estado del pedido', roles: 'ADMINISTRADOR', params: [idParam], body: { type: 'object', required: ['estado'], properties: { estado: str({ enum: ['PENDIENTE', 'LISTO_PARA_RECOGER', 'ENTREGADO', 'CANCELADO'] }) } }, ok: ref('Pedido') }) },

  '/api/admin/productos': { post: op({ tag: 'Admin', summary: 'Crear producto', roles: 'ADMINISTRADOR', status: '201', body: ref('ProductoInput'), ok: ref('Producto') }) },
  '/api/admin/productos/{id}': {
    put: op({ tag: 'Admin', summary: 'Actualizar producto e inventario', roles: 'ADMINISTRADOR', params: [idParam], body: ref('ProductoInput'), ok: ref('Producto') }),
    delete: op({ tag: 'Admin', summary: 'Desactivar producto', roles: 'ADMINISTRADOR', params: [idParam], ok: { type: 'object', properties: { message: str() } } }),
  },
  '/api/admin/productos/{id}/imagen': { post: op({ tag: 'Admin', summary: 'Subir imagen del producto', roles: 'ADMINISTRADOR', params: [idParam], extra: { requestBody: { required: true, content: { 'multipart/form-data': { schema: { type: 'object', properties: { imagen: { type: 'string', format: 'binary' } } } } } } }, ok: ref('Producto') }) },
  '/api/admin/servicios': { post: op({ tag: 'Admin', summary: 'Crear servicio', roles: 'ADMINISTRADOR', status: '201', body: ref('ServicioInput'), ok: ref('Servicio') }) },
  '/api/admin/servicios/{id}': {
    put: op({ tag: 'Admin', summary: 'Actualizar servicio', roles: 'ADMINISTRADOR', params: [idParam], body: ref('ServicioInput'), ok: ref('Servicio') }),
    delete: op({ tag: 'Admin', summary: 'Desactivar servicio', roles: 'ADMINISTRADOR', params: [idParam], ok: { type: 'object', properties: { message: str() } } }),
  },
  '/api/admin/veterinarios': { post: op({ tag: 'Admin', summary: 'Crear veterinario', roles: 'ADMINISTRADOR', status: '201', body: { type: 'object', required: ['nombre', 'email', 'password'], properties: { nombre: str(), email: str(), password: str({ minLength: 8 }), telefono: str() } }, ok: ref('Veterinario') }) },
  '/api/admin/veterinarios/{id}': { put: op({ tag: 'Admin', summary: 'Actualizar veterinario', roles: 'ADMINISTRADOR', params: [idParam], body: { type: 'object', required: ['nombre'], properties: { nombre: str(), telefono: str() } }, ok: ref('Veterinario') }) },
  '/api/admin/veterinarios/{id}/horarios': { put: op({ tag: 'Admin', summary: 'Definir horarios del veterinario', roles: 'ADMINISTRADOR', params: [idParam], body: { type: 'array', items: { type: 'object', required: ['diaSemana', 'horaInicio', 'horaFin'], properties: { diaSemana: int({ minimum: 0, maximum: 6, description: '0=Domingo' }), horaInicio: str({ example: '08:00' }), horaFin: str({ example: '17:00' }) } } }, ok: ref('Veterinario') }) },
  '/api/admin/reportes/ventas': { get: op({ tag: 'Admin', summary: 'Reporte de ventas', roles: 'ADMINISTRADOR', params: [{ name: 'desde', in: 'query', schema: str({ format: 'date' }) }, { name: 'hasta', in: 'query', schema: str({ format: 'date' }) }], ok: ref('ReporteVentas') }) },
  '/api/admin/reportes/citas': { get: op({ tag: 'Admin', summary: 'Reporte de citas', roles: 'ADMINISTRADOR', params: [{ name: 'desde', in: 'query', schema: str({ format: 'date' }) }, { name: 'hasta', in: 'query', schema: str({ format: 'date' }) }], ok: ref('ReporteCitas') }) },
};

const schemas = {
  Error: { type: 'object', properties: { success: { type: 'boolean', example: false }, error: { type: 'object', properties: { code: str({ example: 'VALIDATION_ERROR' }), message: str(), details: { type: 'array', items: { type: 'object' } } } } } },
  Meta: { type: 'object', properties: { page: int(), limit: int(), total: int(), totalPages: int() } },
  Usuario: { type: 'object', properties: { id: int(), nombre: str(), email: str(), telefono: str({ nullable: true }), rol: str({ enum: ['DUENO', 'VETERINARIO', 'ADMINISTRADOR'] }) } },
  Sesion: { type: 'object', properties: { accessToken: str(), refreshToken: str(), expiresIn: int({ description: 'segundos' }), usuario: ref('Usuario') } },
  Especie: { type: 'object', properties: { id: int(), nombre: str() } },
  Vacuna: { type: 'object', properties: { id: int(), nombre: str(), intervaloDias: int() } },
  Servicio: { type: 'object', properties: { id: int(), nombre: str(), precio: num(), duracionMin: int(), activo: { type: 'boolean' } } },
  ServicioInput: { type: 'object', required: ['nombre', 'precio', 'duracionMin'], properties: { nombre: str(), precio: num(), duracionMin: int({ minimum: 15, maximum: 240 }) } },
  VeterinarioResumen: { type: 'object', properties: { id: int(), nombre: str() } },
  Veterinario: { type: 'object', properties: { id: int(), nombre: str(), email: str(), telefono: str({ nullable: true }), horarios: { type: 'array', items: { type: 'object', properties: { diaSemana: int(), horaInicio: str(), horaFin: str() } } } } },
  Mascota: { type: 'object', properties: { id: int(), duenoId: int(), nombre: str(), especie: ref('Especie'), raza: str({ nullable: true }), fechaNacimiento: str({ format: 'date', nullable: true }), pesoKg: num({ nullable: true }), fotoUrl: str({ nullable: true }) } },
  MascotaInput: { type: 'object', required: ['nombre', 'especieId'], properties: { nombre: str(), especieId: int(), raza: str(), fechaNacimiento: str({ format: 'date' }), pesoKg: num() } },
  AplicacionVacuna: { type: 'object', properties: { id: int(), vacuna: ref('VacunaResumen'), fechaAplicacion: str({ format: 'date' }), proximaDosis: str({ format: 'date', nullable: true }), lote: str({ nullable: true }), veterinario: ref('VeterinarioResumen') } },
  VacunaResumen: { type: 'object', properties: { id: int(), nombre: str() } },
  ProximaDosis: { type: 'object', properties: { vacuna: ref('VacunaResumen'), fecha: str({ format: 'date' }) } },
  Consulta: { type: 'object', properties: { id: int(), citaId: int(), fecha: str({ format: 'date' }), diagnostico: str(), tratamiento: str({ nullable: true }), observaciones: str({ nullable: true }), peso: num({ nullable: true }), veterinario: ref('VeterinarioResumen') } },
  Cita: { type: 'object', properties: { id: int(), fecha: str({ format: 'date' }), hora: str(), estado: str({ enum: ['PENDIENTE', 'CONFIRMADA', 'ATENDIDA', 'CANCELADA'] }), mascota: { type: 'object', properties: { id: int(), nombre: str() } }, servicio: ref('Servicio'), veterinario: ref('VeterinarioResumen'), dueno: { type: 'object', properties: { id: int(), nombre: str(), telefono: str({ nullable: true }) } } } },
  Producto: { type: 'object', properties: { id: int(), nombre: str(), descripcion: str({ nullable: true }), categoria: str(), precio: num(), stock: int(), imagenUrl: str({ nullable: true }), activo: { type: 'boolean' } } },
  ProductoInput: { type: 'object', required: ['nombre', 'categoria', 'precio', 'stock'], properties: { nombre: str(), descripcion: str(), categoria: str(), precio: num(), stock: int({ minimum: 0 }) } },
  Pedido: { type: 'object', properties: { id: int(), fecha: str(), estado: str({ enum: ['PENDIENTE', 'LISTO_PARA_RECOGER', 'ENTREGADO', 'CANCELADO'] }), total: num(), items: { type: 'array', items: { type: 'object', properties: { productoId: int(), nombre: str(), precioUnitario: num(), cantidad: int(), subtotal: num() } } }, cliente: ref('VeterinarioResumen') } },
  Notificacion: { type: 'object', properties: { id: int(), titulo: str(), mensaje: str(), leida: { type: 'boolean' }, fecha: str() } },
  ReporteVentas: { type: 'object', properties: { desde: str(), hasta: str(), totalPedidos: int(), totalVentas: num(), productos: { type: 'array', items: { type: 'object', properties: { productoId: int(), nombre: str(), cantidad: int(), total: num() } } } } },
  ReporteCitas: { type: 'object', properties: { desde: str(), hasta: str(), total: int(), porEstado: { type: 'object' }, porServicio: { type: 'array', items: { type: 'object', properties: { servicioId: int(), nombre: str(), cantidad: int() } } } } },
};

module.exports = {
  openapi: '3.0.3',
  info: {
    title: 'PetCare HN - API Veterinaria Huellitas',
    version: '1.0.0',
    description: 'API REST para mascotas, carné de vacunación, citas, tienda y administración. Autenticación JWT (access + refresh). Envíe `Authorization: Bearer <accessToken>`.',
  },
  servers: [{ url: config.publicBaseUrl }],
  tags: ['Auth', 'Usuarios', 'Catálogos', 'Mascotas', 'Citas', 'Tienda', 'Admin'].map((name) => ({ name })),
  components: {
    securitySchemes: { bearerAuth: { type: 'http', scheme: 'bearer', bearerFormat: 'JWT' } },
    schemas,
  },
  paths,
};
