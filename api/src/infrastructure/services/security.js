const crypto = require('crypto');
const bcrypt = require('bcryptjs');
const jwt = require('jsonwebtoken');
const config = require('../../config/env');
const { CODIGO_VERIFICACION_LONGITUD } = require('../../domain/constants');

const ALFABETO_CLAVE = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789';

const passwords = {
  hash: (texto) => bcrypt.hash(texto, 10),
  compare: (texto, hash) => bcrypt.compare(texto, hash),
};

const tokens = {
  accessExpiresInSeconds: config.jwt.accessMinutes * 60,
  signAccess: (usuario) =>
    jwt.sign({ sub: String(usuario.id), rol: usuario.rol }, config.jwt.accessSecret, {
      expiresIn: `${config.jwt.accessMinutes}m`,
    }),
  verifyAccess: (token) => jwt.verify(token, config.jwt.accessSecret),
  newRefreshToken: () => crypto.randomBytes(48).toString('hex'),
};

const hash = (valor) => crypto.createHash('sha256').update(valor).digest('hex');

const generarCodigo = () => {
  const max = 10 ** CODIGO_VERIFICACION_LONGITUD;
  return String(crypto.randomInt(max / 10, max)).padStart(CODIGO_VERIFICACION_LONGITUD, '0');
};

const generarClaveTemporal = () =>
  Array.from({ length: 8 }, () => ALFABETO_CLAVE[crypto.randomInt(ALFABETO_CLAVE.length)]).join('');

module.exports = { passwords, tokens, hash, generarCodigo, generarClaveTemporal };
