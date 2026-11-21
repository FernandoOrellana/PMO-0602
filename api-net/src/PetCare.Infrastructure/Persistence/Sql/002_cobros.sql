ALTER TABLE citas ADD COLUMN precio_servicio_historico DECIMAL(10,2) NULL;

CREATE TABLE cobros (
  id INT AUTO_INCREMENT PRIMARY KEY,
  pedido_id INT NULL,
  cita_id INT NULL,
  cliente_id INT NOT NULL,
  administrador_id INT NOT NULL,
  nombre_cliente VARCHAR(120) NOT NULL,
  nombre_administrador VARCHAR(120) NOT NULL,
  fecha DATETIME NOT NULL,
  importe DECIMAL(10,2) NOT NULL,
  metodo VARCHAR(20) NOT NULL,
  referencia_transferencia VARCHAR(120) NULL,
  clave_idempotencia VARCHAR(64) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  UNIQUE KEY uq_cobros_pedido (pedido_id),
  UNIQUE KEY uq_cobros_cita (cita_id),
  UNIQUE KEY uq_cobros_idempotencia (clave_idempotencia),
  CONSTRAINT chk_cobros_origen CHECK ((pedido_id IS NOT NULL AND cita_id IS NULL) OR (pedido_id IS NULL AND cita_id IS NOT NULL)),
  CONSTRAINT chk_cobros_importe CHECK (importe >= 0),
  CONSTRAINT chk_cobros_metodo CHECK (metodo IN ('EFECTIVO', 'TRANSFERENCIA')),
  CONSTRAINT chk_cobros_referencia CHECK (metodo = 'TRANSFERENCIA' OR referencia_transferencia IS NULL),
  CONSTRAINT chk_cobros_clave CHECK (CHAR_LENGTH(clave_idempotencia) > 0),
  CONSTRAINT fk_cobros_pedido FOREIGN KEY (pedido_id) REFERENCES pedidos(id),
  CONSTRAINT fk_cobros_cita FOREIGN KEY (cita_id) REFERENCES citas(id),
  CONSTRAINT fk_cobros_cliente FOREIGN KEY (cliente_id) REFERENCES usuarios(id),
  CONSTRAINT fk_cobros_administrador FOREIGN KEY (administrador_id) REFERENCES usuarios(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE cobro_items (
  id INT AUTO_INCREMENT PRIMARY KEY,
  cobro_id INT NOT NULL,
  descripcion VARCHAR(160) NOT NULL,
  cantidad INT NOT NULL,
  precio_unitario DECIMAL(10,2) NOT NULL,
  subtotal DECIMAL(10,2) NOT NULL,
  CONSTRAINT chk_cobro_items_cantidad CHECK (cantidad > 0),
  CONSTRAINT chk_cobro_items_precio CHECK (precio_unitario >= 0 AND subtotal >= 0),
  CONSTRAINT fk_cobro_items_cobro FOREIGN KEY (cobro_id) REFERENCES cobros(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
