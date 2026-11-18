class AppError extends Error {
  constructor(message, status, code, details) {
    super(message);
    this.status = status;
    this.code = code;
    this.details = details;
  }
}

class ValidationError extends AppError {
  constructor(message = 'Datos inválidos', details) {
    super(message, 400, 'VALIDATION_ERROR', details);
  }
}

class UnauthorizedError extends AppError {
  constructor(message = 'Autenticación requerida') {
    super(message, 401, 'UNAUTHORIZED');
  }
}

class InvalidCredentialsError extends AppError {
  constructor() {
    super('Correo o contraseña incorrectos', 401, 'INVALID_CREDENTIALS');
  }
}

class ForbiddenError extends AppError {
  constructor(message = 'No tienes permiso para realizar esta acción') {
    super(message, 403, 'FORBIDDEN');
  }
}

class EmailNotVerifiedError extends AppError {
  constructor() {
    super('Debes verificar tu correo antes de iniciar sesión', 403, 'EMAIL_NOT_VERIFIED');
  }
}

class NotFoundError extends AppError {
  constructor(message = 'Recurso no encontrado') {
    super(message, 404, 'NOT_FOUND');
  }
}

class ConflictError extends AppError {
  constructor(message = 'Conflicto con el estado actual del recurso') {
    super(message, 409, 'CONFLICT');
  }
}

module.exports = {
  AppError,
  ValidationError,
  UnauthorizedError,
  InvalidCredentialsError,
  ForbiddenError,
  EmailNotVerifiedError,
  NotFoundError,
  ConflictError,
};
