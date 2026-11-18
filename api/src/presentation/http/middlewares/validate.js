const { ValidationError } = require('../../../shared/errors');

const validar = (esquemas) => (req, _res, next) => {
  const datos = {};
  for (const [origen, esquema] of Object.entries(esquemas)) {
    const resultado = esquema.safeParse(req[origen] ?? {});
    if (!resultado.success) {
      const detalles = resultado.error.issues.map((i) => ({
        campo: i.path.join('.') || origen,
        mensaje: i.message,
      }));
      return next(new ValidationError('Datos inválidos', detalles));
    }
    datos[origen] = resultado.data;
  }
  req.input = datos;
  return next();
};

module.exports = { validar };
