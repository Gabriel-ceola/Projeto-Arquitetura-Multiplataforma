import { createServer, Server } from 'node:http';
import { AddressInfo } from 'node:net';
import { randomUUID } from 'node:crypto';
import { exportJWK, generateKeyPair, SignJWT, type JWK } from 'jose';

export interface JwtTestKitOptions {
  issuer?: string;
  clientId?: string;
  /** Rótulo inicial da chave (kid). Padrão: 'test-key-1'. */
  kid?: string;
}

export interface SignTokenOptions {
  /** Chave de assinatura. Padrão: a privada atual do kit. */
  key?: unknown;
  /** Apelido de `key` (conforme spec das tasks). */
  privateKey?: unknown;
  /** Segredo HMAC (para cenários de confusão de algoritmo, ex. HS256). */
  secret?: Uint8Array | string;
  kid?: string;
  iss?: string;
  azp?: string;
  /** Se omitido, nenhuma claim `aud` é emitida. */
  aud?: string | string[];
  sub?: string;
  roles?: string[];
  /** Onde embutir as roles. Padrão: 'realm'. Aceita `source` como apelido. */
  rolesSource?: 'realm' | 'client';
  source?: 'realm' | 'client';
  clientId?: string;
  alg?: string;
  /** Formatos aceitos pelo jose: '1h', segundos (number), Date. */
  exp?: string | number | Date;
  nbf?: string | number | Date;
  extraClaims?: Record<string, unknown>;
}

export interface JwtTestKit {
  issuer: string;
  clientId: string;
  jwksUri: string;
  url: string;
  kid: string;
  publicJwk: JWK;
  privateKey: unknown;
  signToken(opts?: SignTokenOptions): Promise<string>;
  /** Gera um novo par, publica SOMENTE ele e atualiza os campos do kit. */
  rotate(): Promise<{ privateKey: unknown; publicJwk: JWK; kid: string }>;
  /** Substitui o conjunto publicado (ex.: adicionar segundo kid). */
  setKeys(keys: JWK[]): void;
  close(): Promise<void>;
}

function b64url(obj: unknown): string {
  return Buffer.from(JSON.stringify(obj)).toString('base64url');
}

/**
 * Kit de teste JWT: par RSA + servidor JWKS local efêmero (porta 0).
 *
 * Uso:
 *   const kit = await createJwtTestKit({ issuer, clientId });
 *   const token = await kit.signToken({ sub, roles: ['dentist'] });
 */
export async function createJwtTestKit(
  opts: JwtTestKitOptions = {},
): Promise<JwtTestKit> {
  const issuer = opts.issuer ?? 'https://auth.test/realms/clinica';
  const clientId = opts.clientId ?? 'bff-clinica';

  let currentKid = opts.kid ?? 'test-key-1';
  let currentKeyPair = await generateKeyPair('RS256');
  let currentPublicJwk: JWK = {
    ...(await exportJWK(currentKeyPair.publicKey)),
    kid: currentKid,
    alg: 'RS256',
    use: 'sig',
  };
  let publishedKeys: JWK[] = [{ ...currentPublicJwk }];

  const server: Server = createServer((req, res) => {
    if (req.method === 'GET') {
      res.writeHead(200, { 'content-type': 'application/json' });
      res.end(JSON.stringify({ keys: publishedKeys }));
      return;
    }
    res.writeHead(405, { 'content-type': 'application/json' });
    res.end(JSON.stringify({ message: 'Method not allowed' }));
  });

  await new Promise<void>((resolve) => server.listen(0, '127.0.0.1', resolve));
  const { port } = server.address() as AddressInfo;
  const url = `http://127.0.0.1:${port}`;
  const jwksUri = `${url}/certs`;

  async function signToken(signOpts: SignTokenOptions = {}): Promise<string> {
    const alg = signOpts.alg ?? 'RS256';
    const kid = signOpts.kid ?? (alg === 'none' ? undefined : currentKid);
    const iss = signOpts.iss ?? issuer;
    const sub = signOpts.sub ?? randomUUID();
    const rolesSource = signOpts.rolesSource ?? signOpts.source ?? 'realm';
    const rolesClientId = signOpts.clientId ?? clientId;
    const roles = signOpts.roles ?? ['dentist'];

    const payload: Record<string, unknown> = {
      ...(signOpts.azp !== undefined
        ? { azp: signOpts.azp }
        : { azp: clientId }),
      ...(rolesSource === 'realm'
        ? { realm_access: { roles } }
        : { resource_access: { [rolesClientId]: { roles } } }),
      ...(signOpts.extraClaims ?? {}),
    };

    // Ataque/confusão: alg=none não assina.
    if (alg === 'none') {
      const header = { alg: 'none', typ: 'JWT' };
      return `${b64url(header)}.${b64url({ ...payload, iss, sub })}.`;
    }

    let signingKey: unknown;
    if (signOpts.secret !== undefined) {
      signingKey =
        typeof signOpts.secret === 'string'
          ? new TextEncoder().encode(signOpts.secret)
          : signOpts.secret;
    } else {
      signingKey =
        signOpts.key ?? signOpts.privateKey ?? currentKeyPair.privateKey;
    }

    const jwt = new SignJWT(payload)
      .setProtectedHeader({ alg, ...(kid ? { kid } : {}), typ: 'JWT' })
      .setIssuer(iss)
      .setSubject(sub)
      .setIssuedAt();
    if (signOpts.aud !== undefined) jwt.setAudience(signOpts.aud);
    jwt.setExpirationTime(signOpts.exp ?? '1h');
    if (signOpts.nbf !== undefined) jwt.setNotBefore(signOpts.nbf);
    return jwt.sign(signingKey as never);
  }

  const kit: JwtTestKit = {
    issuer,
    clientId,
    jwksUri,
    url,
    get kid() {
      return currentKid;
    },
    get publicJwk() {
      return currentPublicJwk;
    },
    get privateKey() {
      return currentKeyPair.privateKey;
    },
    signToken,
    async rotate() {
      const pair = await generateKeyPair('RS256');
      currentKid = `test-key-${randomUUID().slice(0, 8)}`;
      currentKeyPair = pair;
      currentPublicJwk = {
        ...(await exportJWK(pair.publicKey)),
        kid: currentKid,
        alg: 'RS256',
        use: 'sig',
      };
      publishedKeys = [{ ...currentPublicJwk }];
      return {
        privateKey: currentKeyPair.privateKey,
        publicJwk: currentPublicJwk,
        kid: currentKid,
      };
    },
    setKeys(keys: JWK[]) {
      publishedKeys = [...keys];
    },
    close() {
      return new Promise<void>((resolve, reject) => {
        server.close((err) => (err ? reject(err) : resolve()));
      });
    },
  };

  return kit;
}
