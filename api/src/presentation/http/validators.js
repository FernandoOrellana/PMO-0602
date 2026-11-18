const { z } = require('zod');
const { ESTADO_CITA, ESTADO_PEDIDO } = require('../../domain/constants');

const id = z.coerce.number().int().positive();
const fecha = z.iso.date();
const hora = z.string().regex(/^([01]\d|2[0-3]):[0-5]\d$/, 'Use el formato HH:mm');
const email = z.email('Correo inválido').transform((v) => v.trim().toLowerCase());
const password = z.string().min(8, 'La contraseña debe tener al menos 8 caracteres').max(100);
const texto = (max) => z.string().trim().min(1).max(max);
const textoOpcional = (max) => z.string().trim().max(max).nullish().transform((v) => v || null);
const dinero = z.coerce.number().positive().max(999999.99);

const paginacion = z.object({
  page: z.coerce.number().int().min(1).default(1),
  limit: z.coerce.number().int().min(1).max(100).default(20),
});

const idParam = z.object({ id });

const schemas = {
  registro: z.object({
    nombre: texto(120),
    email,
    password,
    telefono: textoOpcional(20),
  }),
  verificarEmail: z.object({ email, codigo: z.string().regex(/^\d{6}$/, 'El código debe tener 6 dígitos') }),
  email: z.object({ email }),
  login: z.object({ email, password: z.string().min(1) }),
  refrescar: z.object({ refreshToken: z.string().min(20) }),
  restablecer: z.object({
    email,
    claveTemporal: z.string().min(4).max(50),
    nuevaPassword: password,
  }),
  dispositivo: z.object({ fcmToken: z.string().min(10).max(255) }),

  mascota: z.object({
    nombre: texto(80),
    especieId: id,
    raza: textoOpcional(80),
    fechaNacimiento: fecha.nullish().transform((v) => v || null),
    pesoKg: z.coerce.number().positive().max(300).nullish().transform((v) => v ?? null),
  }),
  aplicacionVacuna: z.object({
    vacunaId: id,
    fechaAplicacion: fecha,
    lote: textoOpcional(60),
    proximaDosis: fecha.nullish().transform((v) => v || null),
    citaId: id.nullish().transform((v) => v ?? null),
  }),

  listaCitas: paginacion.extend({
    fecha: fecha.optional(),
    estado: z.enum(Object.values(ESTADO_CITA)).optional(),
  }),
  cita: z.object({
    mascotaId: id,
    servicioId: id,
    veterinarioId: id,
    fecha,
    hora,
  }),
  consulta: z.object({
    diagnostico: texto(2000),
    tratamiento: textoOpcional(2000),
    observaciones: textoOpcional(2000),
    peso: z.coerce.number().positive().max(300).nullish().transform((v) => v ?? null),
  }),
  disponibilidad: z.object({
    fecha,
    servicioId: id.optional(),
  }),

  listaProductos: paginacion.extend({ categoria: texto(60).optional() }),
  pedido: z.object({
    items: z
      .array(
        z.object({
          productoId: id,
          cantidad: z.coerce.number().int().min(1).max(100),
        })
      )
      .min(1, 'El pedido debe tener al menos un producto')
      .max(50),
  }),
  estadoPedido: z.object({ estado: z.enum(Object.values(ESTADO_PEDIDO)) }),

  producto: z.object({
    nombre: texto(120),
    descripcion: textoOpcional(2000),
    categoria: texto(60),
    precio: dinero,
    stock: z.coerce.number().int().min(0).max(100000),
  }),
  servicio: z.object({
    nombre: texto(120),
    precio: dinero,
    duracionMin: z.coerce.number().int().min(15).max(240),
  }),
  veterinarioCrear: z.object({
    nombre: texto(120),
    email,
    password,
    telefono: textoOpcional(20),
  }),
  veterinarioActualizar: z.object({
    nombre: texto(120),
    telefono: textoOpcional(20),
  }),
  horarios: z
    .array(
      z
        .object({
          diaSemana: z.coerce.number().int().min(0).max(6),
          horaInicio: hora,
          horaFin: hora,
        })
        .refine((h) => h.horaInicio < h.horaFin, 'La hora de inicio debe ser menor que la de fin')
    )
    .max(14),
  rangoReporte: z.object({
    desde: fecha.optional(),
    hasta: fecha.optional(),
  }),
};

module.exports = { schemas, idParam, paginacion };
