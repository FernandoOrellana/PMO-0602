const {
  ValidationError,
  UnauthorizedError,
  ConflictError,
  InvalidCredentialsError,
  EmailNotVerifiedError,
  NotFoundError,
} = require('../../shared/errors');
const { ROLES } = require('../../domain/constants');
const { toUsuario } = require('../../shared/mappers');

module.exports = function createAuthUseCases({ usuarios, refreshTokens, security, mailer, config }) {
  const expiraEnMinutos = (minutos) => new Date(Date.now() + minutos * 60000);

  async function emitirTokens(usuario) {
    const refreshToken = security.tokens.newRefreshToken();
    await refreshTokens.guardar({
      usuarioId: usuario.id,
      tokenHash: security.hash(refreshToken),
      expira: expiraEnMinutos(config.refreshTokenDays * 24 * 60),
    });
    return {
      accessToken: security.tokens.signAccess(usuario),
      refreshToken,
      expiresIn: security.tokens.accessExpiresInSeconds,
      usuario: toUsuario(usuario),
    };
  }

  return {
    async registrar({ nombre, email, password, telefono }) {
      if (await usuarios.buscarPorEmail(email)) {
        throw new ConflictError('Ya existe una cuenta con ese correo');
      }
      const id = await usuarios.crear({
        nombre,
        email,
        telefono,
        passwordHash: await security.passwords.hash(password),
        rol: ROLES.DUENO,
        emailVerificado: false,
      });
      const codigo = security.generarCodigo();
      await usuarios.guardarCodigoVerificacion(
        id,
        security.hash(codigo),
        expiraEnMinutos(config.codigoExpiraMinutos)
      );
      await mailer.enviarCodigoVerificacion(email, codigo);
      return { message: 'Cuenta creada. Revisa tu correo para ingresar el código de verificación.' };
    },

    async verificarEmail({ email, codigo }) {
      const usuario = await usuarios.buscarPorEmail(email);
      if (!usuario) throw new NotFoundError('Usuario no encontrado');
      if (usuario.emailVerificado) return { message: 'La cuenta ya está verificada' };

      const codigoValido =
        usuario.codigoHash &&
        usuario.codigoExpira &&
        usuario.codigoExpira > new Date() &&
        usuario.codigoHash === security.hash(codigo);
      if (!codigoValido) throw new ValidationError('El código es inválido o expiró');

      await usuarios.marcarVerificado(usuario.id);
      return { message: 'Correo verificado. Ya puedes iniciar sesión.' };
    },

    async reenviarCodigo({ email }) {
      const usuario = await usuarios.buscarPorEmail(email);
      if (usuario && !usuario.emailVerificado) {
        const codigo = security.generarCodigo();
        await usuarios.guardarCodigoVerificacion(
          usuario.id,
          security.hash(codigo),
          expiraEnMinutos(config.codigoExpiraMinutos)
        );
        await mailer.enviarCodigoVerificacion(email, codigo);
      }
      return { message: 'Si la cuenta existe y está pendiente, se envió un nuevo código.' };
    },

    async login({ email, password }) {
      const usuario = await usuarios.buscarPorEmail(email);
      if (!usuario || !usuario.activo) throw new InvalidCredentialsError();
      if (!(await security.passwords.compare(password, usuario.passwordHash))) {
        throw new InvalidCredentialsError();
      }
      if (!usuario.emailVerificado) throw new EmailNotVerifiedError();
      return emitirTokens(usuario);
    },

    async refrescar({ refreshToken }) {
      const registro = await refreshTokens.buscarPorHash(security.hash(refreshToken));
      if (!registro || registro.revocado || registro.expira < new Date()) {
        throw new UnauthorizedError('Refresh token inválido o vencido');
      }
      await refreshTokens.revocar(registro.id);
      const usuario = await usuarios.buscarPorId(registro.usuarioId);
      if (!usuario || !usuario.activo) throw new UnauthorizedError('Usuario inactivo');
      return emitirTokens(usuario);
    },

    async cerrarSesion({ refreshToken }) {
      const registro = await refreshTokens.buscarPorHash(security.hash(refreshToken));
      if (registro) await refreshTokens.revocar(registro.id);
      return { message: 'Sesión cerrada' };
    },

    async olvidePassword({ email }) {
      const usuario = await usuarios.buscarPorEmail(email);
      if (usuario && usuario.activo && usuario.emailVerificado) {
        const clave = security.generarClaveTemporal();
        await usuarios.guardarClaveTemporal(
          usuario.id,
          await security.passwords.hash(clave),
          expiraEnMinutos(config.codigoExpiraMinutos)
        );
        await mailer.enviarClaveTemporal(email, clave);
      }
      return { message: 'Si el correo está registrado, recibirás una clave temporal.' };
    },

    async restablecerPassword({ email, claveTemporal, nuevaPassword }) {
      const usuario = await usuarios.buscarPorEmail(email);
      const claveValida =
        usuario &&
        usuario.claveTempHash &&
        usuario.claveTempExpira &&
        usuario.claveTempExpira > new Date() &&
        (await security.passwords.compare(claveTemporal, usuario.claveTempHash));
      if (!claveValida) throw new ValidationError('La clave temporal es inválida o expiró');

      await usuarios.actualizarPassword(usuario.id, await security.passwords.hash(nuevaPassword));
      await refreshTokens.revocarTodosDeUsuario(usuario.id);
      return { message: 'Contraseña actualizada. Inicia sesión con tu nueva contraseña.' };
    },

    async perfil(usuarioId) {
      const usuario = await usuarios.buscarPorId(usuarioId);
      if (!usuario) throw new NotFoundError('Usuario no encontrado');
      return toUsuario(usuario);
    },
  };
};
