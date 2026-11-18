const crypto = require('crypto');
const fs = require('fs');
const path = require('path');
const multer = require('multer');
const config = require('../../../config/env');
const { ValidationError } = require('../../../shared/errors');

const EXTENSIONES = {
  'image/jpeg': '.jpg',
  'image/png': '.png',
  'image/webp': '.webp',
};

const crearUpload = (carpeta, campo) => {
  const destino = path.join(config.uploadDir, carpeta);
  fs.mkdirSync(destino, { recursive: true });
  const upload = multer({
    storage: multer.diskStorage({
      destination: destino,
      filename: (_req, file, cb) => cb(null, `${crypto.randomUUID()}${EXTENSIONES[file.mimetype]}`),
    }),
    limits: { fileSize: 5 * 1024 * 1024 },
    fileFilter: (_req, file, cb) => cb(null, Boolean(EXTENSIONES[file.mimetype])),
  });
  return (req, res, next) =>
    upload.single(campo)(req, res, (err) => {
      if (err) return next(err);
      if (!req.file) {
        return next(new ValidationError('Debes enviar una imagen JPG, PNG o WEBP de máximo 5 MB'));
      }
      req.file.rutaRelativa = `${carpeta}/${req.file.filename}`;
      return next();
    });
};

module.exports = { crearUpload };
