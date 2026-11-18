const ok = (res, data, status = 200) => res.status(status).json({ success: true, data });

const lista = (res, { items, meta }) => res.json({ success: true, data: items, meta });

module.exports = { ok, lista };
