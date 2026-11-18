const { query, transaccion } = require('../database/db');

const horariosDe = async (veterinarioId) =>
  query(
    `SELECT dia_semana AS diaSemana, TIME_FORMAT(hora_inicio, '%H:%i') AS horaInicio,
            TIME_FORMAT(hora_fin, '%H:%i') AS horaFin
     FROM veterinario_horarios WHERE veterinario_id = ?
     ORDER BY dia_semana, hora_inicio`,
    [veterinarioId]
  );

const SELECT_VET = `
  SELECT u.id, u.nombre, u.email, u.telefono FROM usuarios u
  JOIN roles r ON r.id = u.rol_id AND r.nombre = 'VETERINARIO'`;

module.exports = {
  async listar() {
    const vets = await query(`${SELECT_VET} WHERE u.activo = 1 ORDER BY u.nombre`);
    const horarios = await query(
      `SELECT veterinario_id AS veterinarioId, dia_semana AS diaSemana,
              TIME_FORMAT(hora_inicio, '%H:%i') AS horaInicio,
              TIME_FORMAT(hora_fin, '%H:%i') AS horaFin
       FROM veterinario_horarios ORDER BY dia_semana, hora_inicio`
    );
    return vets.map((v) => ({
      ...v,
      telefono: v.telefono ?? null,
      horarios: horarios
        .filter((h) => h.veterinarioId === v.id)
        .map(({ diaSemana, horaInicio, horaFin }) => ({ diaSemana, horaInicio, horaFin })),
    }));
  },

  async buscarPorId(id) {
    const [vet] = await query(`${SELECT_VET} WHERE u.id = ? AND u.activo = 1`, [id]);
    if (!vet) return null;
    return { ...vet, telefono: vet.telefono ?? null, horarios: await horariosDe(id) };
  },

  async actualizarHorarios(id, horarios) {
    await transaccion(async (tx) => {
      await tx.query('DELETE FROM veterinario_horarios WHERE veterinario_id = ?', [id]);
      for (const h of horarios) {
        await tx.query(
          `INSERT INTO veterinario_horarios (veterinario_id, dia_semana, hora_inicio, hora_fin)
           VALUES (?, ?, ?, ?)`,
          [id, h.diaSemana, h.horaInicio, h.horaFin]
        );
      }
    });
  },
};
