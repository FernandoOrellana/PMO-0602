const createAuthUseCases = require('../../src/application/usecases/auth.usecases');
const { ConflictError, InvalidCredentialsError, EmailNotVerifiedError, ValidationError } = require('../../src/shared/errors');
const { ROLES } = require('../../src/domain/constants');

const crearDependencias = (overrides = {}) => {
  const usuarios = {
    buscarPorEmail: jest.fn(),
    buscarPorId: jest.fn(),
    crear: jest.fn().mockResolvedValue(10),
    guardarCodigoVerificacion: jest.fn(),
    marcarVerificado: jest.fn(),
    guardarClaveTemporal: jest.fn(),
    actualizarPassword: jest.fn(),
  };
  const refreshTokens = {
    guardar: jest.fn(),
    buscarPorHash: jest.fn(),
    revocar: jest.fn(),
    revocarTodosDeUsuario: jest.fn(),
  };
  const security = {
    passwords: {
      hash: jest.fn(async (t) => `hash:${t}`),
      compare: jest.fn(async (t, h) => h === `hash:${t}`),
    },
    tokens: {
      signAccess: jest.fn(() => 'access-token'),
      accessExpiresInSeconds: 1800,
      newRefreshToken: jest.fn(() => 'refresh-token'),
    },
    hash: jest.fn((v) => `sha:${v}`),
    generarCodigo: jest.fn(() => '123456'),
    generarClaveTemporal: jest.fn(() => 'Temp1234'),
  };
  const mailer = { enviarCodigoVerificacion: jest.fn(), enviarClaveTemporal: jest.fn() };
  const config = { codigoExpiraMinutos: 15, refreshTokenDays: 7 };
  const deps = { usuarios, refreshTokens, security, mailer, config, ...overrides };
  return { ...deps, auth: createAuthUseCases(deps) };
};

const usuarioBase = {
  id: 5,
  nombre: 'Ana',
  email: 'ana@mail.com',
  telefono: null,
  rol: ROLES.DUENO,
  passwordHash: 'hash:Secreta123',
  emailVerificado: true,
  activo: true,
};

describe('casos de uso de autenticación', () => {
  test('registrar crea un dueño no verificado y envía el código por correo', async () => {
    const { auth, usuarios, mailer } = crearDependencias();
    usuarios.buscarPorEmail.mockResolvedValue(null);

    const resultado = await auth.registrar({ nombre: 'Ana', email: 'ana@mail.com', password: 'Secreta123' });

    expect(usuarios.crear).toHaveBeenCalledWith(
      expect.objectContaining({ rol: ROLES.DUENO, emailVerificado: false })
    );
    expect(usuarios.guardarCodigoVerificacion).toHaveBeenCalledWith(10, 'sha:123456', expect.any(Date));
    expect(mailer.enviarCodigoVerificacion).toHaveBeenCalledWith('ana@mail.com', '123456');
    expect(resultado.message).toMatch(/código/);
  });

  test('registrar rechaza un correo que ya existe', async () => {
    const { auth, usuarios } = crearDependencias();
    usuarios.buscarPorEmail.mockResolvedValue(usuarioBase);

    await expect(
      auth.registrar({ nombre: 'Ana', email: 'ana@mail.com', password: 'Secreta123' })
    ).rejects.toBeInstanceOf(ConflictError);
    expect(usuarios.crear).not.toHaveBeenCalled();
  });

  test('verificarEmail acepta un código correcto y vigente', async () => {
    const { auth, usuarios } = crearDependencias();
    usuarios.buscarPorEmail.mockResolvedValue({
      ...usuarioBase,
      emailVerificado: false,
      codigoHash: 'sha:123456',
      codigoExpira: new Date(Date.now() + 60000),
    });

    await auth.verificarEmail({ email: 'ana@mail.com', codigo: '123456' });

    expect(usuarios.marcarVerificado).toHaveBeenCalledWith(5);
  });

  test('verificarEmail rechaza un código incorrecto', async () => {
    const { auth, usuarios } = crearDependencias();
    usuarios.buscarPorEmail.mockResolvedValue({
      ...usuarioBase,
      emailVerificado: false,
      codigoHash: 'sha:123456',
      codigoExpira: new Date(Date.now() + 60000),
    });

    await expect(auth.verificarEmail({ email: 'ana@mail.com', codigo: '000000' })).rejects.toBeInstanceOf(
      ValidationError
    );
    expect(usuarios.marcarVerificado).not.toHaveBeenCalled();
  });

  test('login rechaza una contraseña incorrecta', async () => {
    const { auth, usuarios } = crearDependencias();
    usuarios.buscarPorEmail.mockResolvedValue(usuarioBase);

    await expect(auth.login({ email: 'ana@mail.com', password: 'otra-clave' })).rejects.toBeInstanceOf(
      InvalidCredentialsError
    );
  });

  test('login exige correo verificado', async () => {
    const { auth, usuarios } = crearDependencias();
    usuarios.buscarPorEmail.mockResolvedValue({ ...usuarioBase, emailVerificado: false });

    await expect(auth.login({ email: 'ana@mail.com', password: 'Secreta123' })).rejects.toBeInstanceOf(
      EmailNotVerifiedError
    );
  });

  test('login emite access token y guarda el refresh token como hash', async () => {
    const { auth, usuarios, refreshTokens } = crearDependencias();
    usuarios.buscarPorEmail.mockResolvedValue(usuarioBase);

    const sesion = await auth.login({ email: 'ana@mail.com', password: 'Secreta123' });

    expect(sesion.accessToken).toBe('access-token');
    expect(sesion.refreshToken).toBe('refresh-token');
    expect(sesion.expiresIn).toBe(1800);
    expect(sesion.usuario).toEqual({ id: 5, nombre: 'Ana', email: 'ana@mail.com', telefono: null, rol: ROLES.DUENO });
    expect(refreshTokens.guardar).toHaveBeenCalledWith(
      expect.objectContaining({ usuarioId: 5, tokenHash: 'sha:refresh-token' })
    );
  });

  test('restablecerPassword rechaza una clave temporal vencida', async () => {
    const { auth, usuarios } = crearDependencias();
    usuarios.buscarPorEmail.mockResolvedValue({
      ...usuarioBase,
      claveTempHash: 'hash:Temp1234',
      claveTempExpira: new Date(Date.now() - 1000),
    });

    await expect(
      auth.restablecerPassword({ email: 'ana@mail.com', claveTemporal: 'Temp1234', nuevaPassword: 'NuevaClave1' })
    ).rejects.toBeInstanceOf(ValidationError);
    expect(usuarios.actualizarPassword).not.toHaveBeenCalled();
  });

  test('restablecerPassword actualiza la contraseña y revoca las sesiones', async () => {
    const { auth, usuarios, refreshTokens } = crearDependencias();
    usuarios.buscarPorEmail.mockResolvedValue({
      ...usuarioBase,
      claveTempHash: 'hash:Temp1234',
      claveTempExpira: new Date(Date.now() + 60000),
    });

    await auth.restablecerPassword({ email: 'ana@mail.com', claveTemporal: 'Temp1234', nuevaPassword: 'NuevaClave1' });

    expect(usuarios.actualizarPassword).toHaveBeenCalledWith(5, 'hash:NuevaClave1');
    expect(refreshTokens.revocarTodosDeUsuario).toHaveBeenCalledWith(5);
  });
});
