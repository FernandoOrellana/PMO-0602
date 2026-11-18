const { autenticar, autorizar } = require('../../src/presentation/http/middlewares/auth');
const { ROLES } = require('../../src/domain/constants');
const { UnauthorizedError, ForbiddenError } = require('../../src/shared/errors');

const tokens = {
  verifyAccess: jest.fn((token) => {
    if (token === 'valido-dueno') return { sub: '5', rol: ROLES.DUENO };
    throw new Error('invalid signature');
  }),
};

const ejecutar = (middleware, req) =>
  new Promise((resolve) => middleware(req, {}, (err) => resolve(err)));

describe('autorización por roles', () => {
  test('rechaza una petición sin token con 401', async () => {
    const err = await ejecutar(autenticar(tokens), { headers: {} });
    expect(err).toBeInstanceOf(UnauthorizedError);
    expect(err.status).toBe(401);
  });

  test('rechaza un token inválido o vencido con 401', async () => {
    const err = await ejecutar(autenticar(tokens), { headers: { authorization: 'Bearer falso' } });
    expect(err).toBeInstanceOf(UnauthorizedError);
  });

  test('acepta un token válido y carga el usuario en la petición', async () => {
    const req = { headers: { authorization: 'Bearer valido-dueno' } };
    const err = await ejecutar(autenticar(tokens), req);
    expect(err).toBeUndefined();
    expect(req.user).toEqual({ id: 5, rol: ROLES.DUENO });
  });

  test('deniega con 403 a un dueño que intenta una acción de veterinario', async () => {
    const err = await ejecutar(autorizar(ROLES.VETERINARIO), { user: { id: 5, rol: ROLES.DUENO } });
    expect(err).toBeInstanceOf(ForbiddenError);
    expect(err.status).toBe(403);
  });

  test('permite la acción cuando el rol coincide', async () => {
    const err = await ejecutar(autorizar(ROLES.ADMINISTRADOR), { user: { id: 1, rol: ROLES.ADMINISTRADOR } });
    expect(err).toBeUndefined();
  });
});
