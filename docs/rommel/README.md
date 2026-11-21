# Verificacion de correo (Rommel)

Flujo: registro -> codigo de 6 digitos por correo -> verificacion -> login.

Endpoints:
- POST /api/auth/register
- POST /api/auth/verify-email
- POST /api/auth/resend-code

El codigo expira en 15 minutos (App:CodigoExpiraMinutos).

Configuracion SMTP (variables de entorno, sin claves en el repo):
Smtp__Host, Smtp__Port, Smtp__Secure, Smtp__User, Smtp__Password, Smtp__From
