import {
  createServer,
  IncomingMessage,
  Server,
  ServerResponse,
} from 'node:http';
import { AddressInfo } from 'node:net';
import { randomUUID } from 'node:crypto';

export interface MockDentistService {
  id: string;
  name: string;
  dentistId: string;
  price: number;
  durationMinutes: number;
  isPeriodic: boolean;
  status: string;
  createdAt: string;
  updatedAt: string;
}

export interface RecordedRequest {
  method: string;
  path: string;
  headers: Record<string, string | string[] | undefined>;
  body: unknown;
}

export interface DownstreamMockOptions {
  seed?: Array<Partial<MockDentistService> & { dentistId: string }>;
}

const BASE = '/api/dentist-services';

function nowIso(): string {
  return new Date().toISOString();
}

function sendJson(res: ServerResponse, status: number, body: unknown): void {
  const payload = body === undefined ? '' : JSON.stringify(body);
  res.writeHead(status, { 'content-type': 'application/json' });
  res.end(payload);
}

function readBody(req: IncomingMessage): Promise<unknown> {
  return new Promise((resolve) => {
    let raw = '';
    req.on('data', (chunk) => {
      raw += chunk;
    });
    req.on('end', () => {
      if (!raw) return resolve(undefined);
      try {
        resolve(JSON.parse(raw));
      } catch {
        resolve(raw);
      }
    });
  });
}

/**
 * Mock HTTP do Dentist Service (contrato openapi.yml).
 * Escuta em porta 0; registra todas as requisições recebidas.
 */
export async function createDownstreamMock(opts: DownstreamMockOptions = {}) {
  const items = new Map<string, MockDentistService>();
  const requests: RecordedRequest[] = [];
  let forced: { status: number; body: unknown } | undefined;
  let delayMs = 0;

  const seed = (
    entries: Array<Partial<MockDentistService> & { dentistId: string }>,
  ): MockDentistService[] => {
    return entries.map((e) => {
      const item: MockDentistService = {
        id: e.id ?? randomUUID(),
        name: e.name ?? 'Limpeza completa',
        dentistId: e.dentistId,
        price: e.price ?? 150,
        durationMinutes: e.durationMinutes ?? 30,
        isPeriodic: e.isPeriodic ?? false,
        status: e.status ?? 'disponivel',
        createdAt: e.createdAt ?? nowIso(),
        updatedAt: e.updatedAt ?? nowIso(),
      };
      items.set(item.id, item);
      return item;
    });
  };

  if (opts.seed) seed(opts.seed);

  const server: Server = createServer(async (req, res) => {
    const method = (req.method ?? 'GET').toUpperCase();
    const rawUrl = req.url ?? '/';
    const path = rawUrl.split('?')[0];
    const body = await readBody(req);

    requests.push({ method, path, headers: { ...req.headers }, body });

    if (delayMs > 0) {
      await new Promise((r) => setTimeout(r, delayMs));
    }

    if (forced) {
      const f = forced;
      forced = undefined;
      sendJson(res, f.status, f.body);
      return;
    }

    const dentistId = req.headers['x-dentist-id'] as string | undefined;

    // GET lista — filtra por X-Dentist-Id quando presente
    if (method === 'GET' && path === BASE) {
      const list = [...items.values()].filter((i) =>
        dentistId ? i.dentistId === dentistId : true,
      );
      sendJson(res, 200, list);
      return;
    }

    // POST — cria com dentistId do body
    if (method === 'POST' && path === BASE) {
      const b = (body ?? {}) as Record<string, unknown>;
      const item: MockDentistService = {
        id: randomUUID(),
        name: String(b['name'] ?? 'Serviço'),
        dentistId: String(b['dentistId'] ?? dentistId ?? 'unknown'),
        price: Number(b['price'] ?? 100),
        durationMinutes: Number(b['durationMinutes'] ?? 30),
        isPeriodic: Boolean(b['isPeriodic'] ?? false),
        status: String(b['status'] ?? 'disponivel'),
        createdAt: nowIso(),
        updatedAt: nowIso(),
      };
      items.set(item.id, item);
      sendJson(res, 201, item);
      return;
    }

    // Rotas com :id
    if (path.startsWith(`${BASE}/`)) {
      const id = decodeURIComponent(path.slice(BASE.length + 1));
      const item = items.get(id);

      if (method === 'GET' || method === 'DELETE') {
        if (!item) {
          sendJson(res, 404, { message: 'Serviço não encontrado.' });
          return;
        }
        if (dentistId && item.dentistId !== dentistId) {
          sendJson(res, 403, { message: 'Acesso negado a este recurso.' });
          return;
        }
        if (method === 'GET') {
          sendJson(res, 200, item);
          return;
        }
        items.delete(id);
        res.writeHead(204);
        res.end();
        return;
      }

      // PUT — espelha o contrato real (§7.2): sem checagem de ownership
      if (method === 'PUT') {
        if (!item) {
          sendJson(res, 404, { message: 'Serviço não encontrado.' });
          return;
        }
        const b = (body ?? {}) as Record<string, unknown>;
        const updated: MockDentistService = {
          ...item,
          name: b['name'] !== undefined ? String(b['name']) : item.name,
          price: b['price'] !== undefined ? Number(b['price']) : item.price,
          durationMinutes:
            b['durationMinutes'] !== undefined
              ? Number(b['durationMinutes'])
              : item.durationMinutes,
          isPeriodic:
            b['isPeriodic'] !== undefined
              ? Boolean(b['isPeriodic'])
              : item.isPeriodic,
          status: b['status'] !== undefined ? String(b['status']) : item.status,
          updatedAt: nowIso(),
        };
        items.set(id, updated);
        sendJson(res, 200, updated);
        return;
      }
    }

    sendJson(res, 404, { message: 'Rota não encontrada no mock.' });
  });

  await new Promise<void>((resolve) => server.listen(0, '127.0.0.1', resolve));
  const { port } = server.address() as AddressInfo;

  return {
    baseUrl: `http://127.0.0.1:${port}`,
    requests,
    seed,
    /** Força a PRÓXIMA resposta (qualquer rota) com status/body dados. */
    forceNext(status: number, body: unknown) {
      forced = { status, body };
    },
    /** Atrasa todas as respostas em `ms` (0 = sem atraso). */
    setDelay(ms: number) {
      delayMs = ms;
    },
    /** Limpa requisições registradas, forceNext e delay (mantém os dados). */
    reset() {
      requests.length = 0;
      forced = undefined;
      delayMs = 0;
    },
    /** Limpa requisições E dados. */
    clear() {
      requests.length = 0;
      forced = undefined;
      delayMs = 0;
      items.clear();
    },
    close(): Promise<void> {
      return new Promise<void>((resolve, reject) => {
        server.close((err) => (err ? reject(err) : resolve()));
      });
    },
  };
}

export type DownstreamMock = Awaited<ReturnType<typeof createDownstreamMock>>;
