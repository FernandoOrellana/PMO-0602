const { UnauthorizedError, ForbiddenError } = require('../../../shared/errors');

const autenticar = (tokens) => (req, _res, next) => {
  const [tipo, token] = (req.headers.authorization || '').split(' ');
  if (tipo !== 'Bearer' || !token) return next(new UnauthorizedError('Token requerido'));
  try {
    const payload = tokens.verifyAccess(token);
    req.user = { id: Number(payload.sub), rol: payload.rol };
    return next();
  } catch {
    return next(new UnauthorizedError('Token inválido o vencido'));
  }
};

const autorizar =
  (...roles) =>
  (req, _res, next) => {
    if (!req.user) return next(new UnauthorizedError());
    if (!roles.includes(req.user.rol)) {
      return next(new ForbiddenError('Tu rol no tiene permiso para esta acción'));
    }
    return next();
  };

module.exports = { autenticar, autorizar };
