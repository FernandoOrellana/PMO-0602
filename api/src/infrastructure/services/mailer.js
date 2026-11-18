const nodemailer = require('nodemailer');
const config = require('../../config/env');

const transporter = config.smtp.host
  ? nodemailer.createTransport({
      host: config.smtp.host,
      port: config.smtp.port,
      secure: config.smtp.secure,
      auth: config.smtp.user ? { user: config.smtp.user, pass: config.smtp.pass } : undefined,
    })
  : null;

const enviar = async ({ to, subject, text }) => {
  if (!transporter) {
    console.log(`[correo-dev] Para: ${to}\n[correo-dev] Asunto: ${subject}\n${text}`);
    return;
  }
  await transporter.sendMail({ from: config.smtp.from, to, subject, text });
};

module.exports = {
  enviarCodigoVerificacion: (email, codigo) =>
    enviar({
      to: email,
      subject: 'Código de verificación - Veterinaria Huellitas',
      text: `Tu código de verificación es: ${codigo}\nExpira en ${config.codigoExpiraMinutos} minutos.`,
    }),

  enviarClaveTemporal: (email, clave) =>
    enviar({
      to: email,
      subject: 'Clave temporal - Veterinaria Huellitas',
      text: `Tu clave temporal es: ${clave}\nExpira en ${config.codigoExpiraMinutos} minutos. Úsala para restablecer tu contraseña.`,
    }),
};
