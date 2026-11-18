const fs = require('fs');
const path = require('path');
const config = require('../../config/env');

module.exports = {
  async eliminar(ruta) {
    if (!ruta) return;
    const absoluta = path.join(config.uploadDir, ruta);
    if (!absoluta.startsWith(config.uploadDir)) return;
    await fs.promises.rm(absoluta, { force: true });
  },
};
