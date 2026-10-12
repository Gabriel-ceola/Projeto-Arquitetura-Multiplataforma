// Mock standalone do Dentist Service para `docker compose` (dev).
// Uso: node server.js (PORT env, padrão 5068).
const http = require('http');

const items = new Map();
const port = Number(process.env.PORT || 5068);

function send(res, status, body) {
  res.writeHead(status, { 'content-type': 'application/json' });
  res.end(body === undefined ? '' : JSON.stringify(body));
}

const server = http.createServer((req, res) => {
  const url = new URL(req.url, 'http://localhost');
  const dentistId = req.headers['x-dentist-id'];
  let raw = '';
  req.on('data', (c) => (raw += c));
  req.on('end', () => {
    const body = raw ? JSON.parse(raw) : undefined;
    const m = url.pathname.match(/^\/api\/dentist-services(?:\/([^/]+))?$/);
    if (!m) return send(res, 404, { message: 'Não encontrado.' });
    const id = m[1];
    if (req.method === 'GET' && !id) {
      return send(
        res,
        200,
        [...items.values()].filter((i) => i.dentistId === dentistId),
      );
    }
    if (req.method === 'GET' && id) {
      const it = items.get(id);
      if (!it) return send(res, 404, { message: 'Serviço não encontrado.' });
      if (it.dentistId !== dentistId)
        return send(res, 403, { message: 'Acesso negado ao serviço.' });
      return send(res, 200, it);
    }
    if (req.method === 'POST' && !id) {
      const now = new Date().toISOString();
      const created = {
        id: `${Date.now()}`,
        ...body,
        createdAt: now,
        updatedAt: now,
      };
      items.set(created.id, created);
      return send(res, 201, created);
    }
    if ((req.method === 'PUT' || req.method === 'DELETE') && id) {
      const it = items.get(id);
      if (!it) return send(res, 404, { message: 'Serviço não encontrado.' });
      if (it.dentistId !== dentistId)
        return send(res, 403, { message: 'Acesso negado ao serviço.' });
      if (req.method === 'DELETE') {
        items.delete(id);
        res.writeHead(204);
        return res.end();
      }
      const updated = { ...it, ...body, updatedAt: new Date().toISOString() };
      items.set(id, updated);
      return send(res, 200, updated);
    }
    return send(res, 404, { message: 'Não encontrado.' });
  });
});

server.listen(port, () => console.log(`mock listening on ${port}`));
