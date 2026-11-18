const fs = require('fs');
const multer = require('multer');
const { AppError, ValidationError } = require('../../../shared/errors');

const responderError = (res, status, code, message, details) =>
  res.status(status).json({
    success: false,
    error: { code, message, ...(details ? { details } : {}) },
  });

const notFound = (req, res) =>
  responderError(res, 404, 'NOT_FOUND', `Ruta no encontrada: ${req.method} ${req.originalUrl}`);

const borrarArchivoSubido = (req) => {
  if (req.file?.path) fs.promises.rm(req.file.path, { force: true }).catch(() => {});
};

const errorHandler = (err, req, res, _next) => {
  borrarArchivoSubido(req);
  if (err instanceof AppError) {
    return responderError(res, err.status, err.code, err.message, err.details);
  }
  if (err instanceof multer.MulterError) {
    const v = new ValidationError('Archivo inválido o demasiado grande');
    return responderError(res, v.status, v.code, v.message);
  }
  if (err.type === 'entity.parse.failed') {
    return responderError(res, 400, 'VALIDATION_ERROR', 'El cuerpo de la petición no es JSON válido');
  }
  if (err.code === 'ER_DUP_ENTRY') {
    return responderError(res, 409, 'CONFLICT', 'Ya existe un registro con esos datos');
  }
  console.error(err);
  return responderError(res, 500, 'INTERNAL_ERROR', 'Error interno del servidor');
};

module.exports = { errorHandler, notFound };
