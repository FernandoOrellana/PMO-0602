const fs = require('fs');
const path = require('path');
const openapi = require('../src/presentation/docs/openapi');

const SALIDA = path.resolve(__dirname, '../docs/PetCare-HN.postman_collection.json');

const variable = (key, value) => ({ key, value, type: 'string' });

const itemDe = (ruta, metodo, operacion) => {
  const url = {
    raw: `{{baseUrl}}${ruta.replace(/\{(\w+)\}/g, ':$1')}`,
    host: ['{{baseUrl}}'],
    path: ruta
      .replace(/^\/api\//, 'api/')
      .split('/')
      .filter(Boolean)
      .map((seg) => seg.replace(/\{(\w+)\}/g, ':$1')),
    query: (operacion.parameters || [])
      .filter((p) => p.in === 'query')
      .map((p) => ({ key: p.name, value: '', disabled: !p.required })),
    variable: (operacion.parameters || [])
      .filter((p) => p.in === 'path')
      .map((p) => ({ key: p.name, value: `{{${p.name}}}` })),
  };

  const request = {
    method: metodo.toUpperCase(),
    header: [],
    url,
  };

  if (operacion.security && operacion.security.length) {
    request.auth = { type: 'bearer', bearer: [{ key: 'token', value: '{{accessToken}}', type: 'string' }] };
  } else {
    request.auth = { type: 'noauth' };
  }

  if (operacion.requestBody) {
    const tipo = Object.keys(operacion.requestBody.content)[0];
    if (tipo === 'application/json') {
      request.header.push({ key: 'Content-Type', value: 'application/json' });
      request.body = { mode: 'raw', raw: '{}', options: { raw: { language: 'json' } } };
    } else {
      request.body = { mode: 'formdata', formdata: [{ key: Object.keys(operacion.requestBody.content[tipo].schema.properties)[0], type: 'file', src: '' }] };
    }
  }

  return { name: `${operacion.summary}`, request };
};

const carpetas = {};
for (const [ruta, metodos] of Object.entries(openapi.paths)) {
  for (const [metodo, operacion] of Object.entries(metodos)) {
    const tag = operacion.tags[0];
    carpetas[tag] = carpetas[tag] || { name: tag, item: [] };
    carpetas[tag].item.push(itemDe(ruta, metodo, operacion));
  }
}

const coleccion = {
  info: {
    name: 'PetCare HN - Veterinaria Huellitas',
    description: openapi.info.description,
    schema: 'https://schema.getpostman.com/json/collection/v2.1.0/collection.json',
  },
  variable: [
    variable('baseUrl', openapi.servers[0].url),
    variable('accessToken', ''),
    variable('refreshToken', ''),
  ],
  item: Object.values(carpetas),
};

fs.mkdirSync(path.dirname(SALIDA), { recursive: true });
fs.writeFileSync(SALIDA, JSON.stringify(coleccion, null, 2));
console.log(`Colección generada: ${SALIDA}`);
