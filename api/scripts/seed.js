const config = require('../src/config/env');
const { query, pool } = require('../src/infrastructure/database/db');
const security = require('../src/infrastructure/services/security');
const { ROLES } = require('../src/domain/constants');
const { calcularProximaDosis } = require('../src/domain/rules');

const ESPECIES = ['Perro', 'Gato', 'Ave', 'Conejo', 'Otro'];

const VACUNAS = [
  { nombre: 'Antirrábica', intervaloDias: 365 },
  { nombre: 'Séxtuple', intervaloDias: 365 },
  { nombre: 'Desparasitación', intervaloDias: 90 },
];

const SERVICIOS = [
  { nombre: 'Consulta general', precio: 350, duracionMin: 30 },
  { nombre: 'Vacuna antirrábica', precio: 300, duracionMin: 30 },
  { nombre: 'Vacuna séxtuple', precio: 450, duracionMin: 30 },
  { nombre: 'Desparasitación', precio: 200, duracionMin: 30 },
  { nombre: 'Baño y peluquería', precio: 400, duracionMin: 60 },
];

const PRODUCTOS = [
  { nombre: 'Alimento para perro adulto 15 kg', categoria: 'Alimento', precio: 1250, stock: 20 },
  { nombre: 'Alimento para gato adulto 3 kg', categoria: 'Alimento', precio: 480, stock: 25 },
  { nombre: 'Collar ajustable', categoria: 'Accesorios', precio: 150, stock: 40 },
  { nombre: 'Correa retráctil 5 m', categoria: 'Accesorios', precio: 320, stock: 15 },
  { nombre: 'Shampoo para mascotas', categoria: 'Higiene', precio: 210, stock: 30 },
  { nombre: 'Juguete de goma', categoria: 'Juguetes', precio: 95, stock: 50 },
];

const HORARIO_SEMANA = [1, 2, 3, 4, 5].map((diaSemana) => ({
  diaSemana,
  horaInicio: '08:00',
  horaFin: '17:00',
}));

const HORARIO_SABADO = [{ diaSemana: 6, horaInicio: '08:00', horaFin: '12:00' }];

async function sembrarRoles() {
  for (const rol of Object.values(ROLES)) {
    await query('INSERT IGNORE INTO roles (nombre) VALUES (?)', [rol]);
  }
}

async function sembrarCatalogos() {
  for (const nombre of ESPECIES) {
    await query('INSERT IGNORE INTO especies (nombre) VALUES (?)', [nombre]);
  }
  for (const v of VACUNAS) {
    await query('INSERT IGNORE INTO vacunas (nombre, intervalo_dias) VALUES (?, ?)', [
      v.nombre,
      v.intervaloDias,
    ]);
  }
  for (const s of SERVICIOS) {
    await query(
      'INSERT IGNORE INTO servicios (nombre, precio, duracion_min) VALUES (?, ?, ?)',
      [s.nombre, s.precio, s.duracionMin]
    );
  }
  for (const p of PRODUCTOS) {
    const [existe] = await query('SELECT id FROM productos WHERE nombre = ?', [p.nombre]);
    if (!existe) {
      await query(
        'INSERT INTO productos (nombre, categoria, precio, stock) VALUES (?, ?, ?, ?)',
        [p.nombre, p.categoria, p.precio, p.stock]
      );
    }
  }
}

async function crearUsuario({ nombre, email, password, rol, telefono = null }) {
  const [existente] = await query('SELECT id FROM usuarios WHERE email = ?', [email]);
  if (existente) return existente.id;
  const r = await query(
    `INSERT INTO usuarios (rol_id, nombre, email, telefono, password_hash, email_verificado)
     VALUES ((SELECT id FROM roles WHERE nombre = ?), ?, ?, ?, ?, 1)`,
    [rol, nombre, email, telefono, await security.passwords.hash(password)]
  );
  return r.insertId;
}

async function fijarHorarios(veterinarioId, horarios) {
  await query('DELETE FROM veterinario_horarios WHERE veterinario_id = ?', [veterinarioId]);
  for (const h of horarios) {
    await query(
      `INSERT INTO veterinario_horarios (veterinario_id, dia_semana, hora_inicio, hora_fin)
       VALUES (?, ?, ?, ?)`,
      [veterinarioId, h.diaSemana, h.horaInicio, h.horaFin]
    );
  }
}

async function sembrarDatosDemo(vetId) {
  const duenoId = await crearUsuario({
    nombre: 'Cliente Demo',
    email: 'dueno@demo.hn',
    password: config.seed.demoPassword,
    rol: ROLES.DUENO,
    telefono: '9999-0000',
  });

  const [{ id: especieId }] = await query("SELECT id FROM especies WHERE nombre = 'Perro'");
  const [mascotaExistente] = await query(
    "SELECT id FROM mascotas WHERE dueno_id = ? AND nombre = 'Firulais'",
    [duenoId]
  );
  if (!mascotaExistente) {
    const r = await query(
      `INSERT INTO mascotas (dueno_id, especie_id, nombre, raza, fecha_nacimiento, peso_kg)
       VALUES (?, ?, 'Firulais', 'Labrador', '2020-03-15', 25.5)`,
      [duenoId, especieId]
    );
    const [{ id: vacunaId, intervaloDias }] = await query(
      "SELECT id, intervalo_dias AS intervaloDias FROM vacunas WHERE nombre = 'Antirrábica'"
    );
    const fechaAplicacion = '2026-01-10';
    await query(
      `INSERT INTO aplicaciones_vacuna (mascota_id, vacuna_id, veterinario_id, fecha_aplicacion, proxima_dosis, lote)
       VALUES (?, ?, ?, ?, ?, 'LOTE-DEMO')`,
      [
        r.insertId,
        vacunaId,
        vetId,
        fechaAplicacion,
        calcularProximaDosis(fechaAplicacion, intervaloDias),
      ]
    );
  }
}

async function sembrar() {
  await sembrarRoles();
  await sembrarCatalogos();

  if (!config.seed.adminPassword) {
    throw new Error('Define SEED_ADMIN_PASSWORD en el archivo .env antes de ejecutar el seed');
  }

  await crearUsuario({
    nombre: 'Administrador',
    email: config.seed.adminEmail,
    password: config.seed.adminPassword,
    rol: ROLES.ADMINISTRADOR,
  });

  const vetId = await crearUsuario({
    nombre: 'Dra. Ana Martínez',
    email: 'veterinario@demo.hn',
    password: config.seed.demoPassword,
    rol: ROLES.VETERINARIO,
    telefono: '9999-1111',
  });
  await fijarHorarios(vetId, [...HORARIO_SEMANA, ...HORARIO_SABADO]);

  const vet2Id = await crearUsuario({
    nombre: 'Dr. Carlos Reyes',
    email: 'veterinario2@demo.hn',
    password: config.seed.demoPassword,
    rol: ROLES.VETERINARIO,
    telefono: '9999-2222',
  });
  await fijarHorarios(vet2Id, HORARIO_SEMANA);

  await sembrarDatosDemo(vetId);

  console.log('Datos iniciales cargados:');
  console.log(`  Administrador: ${config.seed.adminEmail}`);
  console.log('  Veterinario:   veterinario@demo.hn (clave demo)');
  console.log('  Dueño:         dueno@demo.hn (clave demo)');
  console.log(`  Clave demo:    ${config.seed.demoPassword}`);
}

sembrar()
  .catch((err) => {
    console.error('Error en seed:', err.message);
    process.exitCode = 1;
  })
  .finally(() => pool.end());
