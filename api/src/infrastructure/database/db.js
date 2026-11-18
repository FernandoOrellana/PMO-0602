const mysql = require('mysql2/promise');
const config = require('../../config/env');

const pool = mysql.createPool({
  host: config.db.host,
  port: config.db.port,
  user: config.db.user,
  password: config.db.password,
  database: config.db.database,
  waitForConnections: true,
  connectionLimit: 10,
  decimalNumbers: true,
  dateStrings: ['DATE'],
});

const query = async (sql, params = []) => {
  const [result] = await pool.query(sql, params);
  return result;
};

const transaccion = async (fn) => {
  const conn = await pool.getConnection();
  try {
    await conn.beginTransaction();
    const tx = {
      query: async (sql, params = []) => {
        const [result] = await conn.query(sql, params);
        return result;
      },
    };
    const resultado = await fn(tx);
    await conn.commit();
    return resultado;
  } catch (err) {
    await conn.rollback();
    throw err;
  } finally {
    conn.release();
  }
};

module.exports = { pool, query, transaccion };
