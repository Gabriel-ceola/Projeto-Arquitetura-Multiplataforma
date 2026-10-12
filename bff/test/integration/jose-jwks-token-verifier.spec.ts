import { randomUUID } from 'node:crypto';
import { exportJWK, generateKeyPair, SignJWT } from 'jose';
import { JoseJwksTokenVerifier } from '../../src/modules/auth/infrastructure/jose-jwks-token-verifier.adapter';
import { InvalidTokenError } from '../../src/modules/auth/domain/auth-errors';
import { Role } from '../../src/modules/auth/domain/role.enum';
import { buildTestConfig } from '../support/test-config';
import { createJwtTestKit, type JwtTestKit } from '../support/jwt-test-kit';

describe('JoseJwksTokenVerifier (integração + JWKS local)', () => {
  let kit: JwtTestKit;

  beforeAll(async () => {
    kit = await createJwtTestKit({});
  });

  afterAll(async () => {
    // O kit pode já ter sido fechado pelo cenário "JWKS fora do ar".
    await kit.close().catch(() => undefined);
  });

  function buildVerifier() {
    const config = buildTestConfig({
      KEYCLOAK_ISSUER: kit.issuer,
      KEYCLOAK_JWKS_URI: kit.jwksUri,
      KEYCLOAK_CLIENT_ID: kit.clientId,
      JWKS_CACHE_MAX_AGE_MS: 1000,
      JWKS_COOLDOWN_MS: 0,
      DOWNSTREAM_TIMEOUT_MS: 2000,
    });
    return new JoseJwksTokenVerifier(config);
  }

  /** Nenhum erro/log pode conter o token (anti-vazamento §11). */
  function expectNoLeak(token: string, err: unknown) {
    expect(err).toBeInstanceOf(InvalidTokenError);
    expect(String((err as Error).message)).not.toContain(token);
    expect(String((err as Error).message).length).toBeLessThan(token.length);
  }

  it('aceita token válido e devolve AuthenticatedUser (id=sub, roles)', async () => {
    // Arrange
    const sub = randomUUID();
    const sut = buildVerifier();
    const token = await kit.signToken({ sub, roles: ['dentist'] });

    // Act
    const user = await sut.verify(token);

    // Assert
    expect(user.id).toBe(sub);
    expect(user.roles).toEqual([Role.Dentist]);
  });

  it('rejeita token expirado', async () => {
    // Arrange
    const sub = randomUUID();
    const sut = buildVerifier();
    const token = await kit.signToken({
      sub,
      roles: ['dentist'],
      exp: Math.floor(Date.now() / 1000) - 60,
    });

    // Act
    const act = sut.verify(token);

    // Assert
    await expect(act).rejects.toBeInstanceOf(InvalidTokenError);
    await act.catch((err) => expectNoLeak(token, err));
  });

  it('rejeita token com nbf no futuro', async () => {
    // Arrange
    const sub = randomUUID();
    const sut = buildVerifier();
    const token = await kit.signToken({
      sub,
      roles: ['dentist'],
      nbf: Math.floor(Date.now() / 1000) + 120,
    });

    // Act
    const act = sut.verify(token);

    // Assert
    await expect(act).rejects.toBeInstanceOf(InvalidTokenError);
    await act.catch((err) => expectNoLeak(token, err));
  });

  it('aceita token dentro da tolerância de relógio', async () => {
    // Arrange: expirado há 3s, tolerância padrão de 5s
    const sub = randomUUID();
    const sut = buildVerifier();
    const token = await kit.signToken({
      sub,
      roles: ['dentist'],
      exp: Math.floor(Date.now() / 1000) - 3,
    });

    // Act
    const user = await sut.verify(token);

    // Assert
    expect(user.id).toBe(sub);
  });

  it('rejeita token com iss diferente', async () => {
    // Arrange
    const sub = randomUUID();
    const sut = buildVerifier();
    const token = await kit.signToken({
      sub,
      roles: ['dentist'],
      iss: 'https://auth.malicioso.test/realms/outro',
    });

    // Act
    const act = sut.verify(token);

    // Assert
    await expect(act).rejects.toBeInstanceOf(InvalidTokenError);
    await act.catch((err) => expectNoLeak(token, err));
  });

  it('rejeita token com azp diferente (sem audience configurada)', async () => {
    // Arrange
    const sub = randomUUID();
    const sut = buildVerifier();
    const token = await kit.signToken({
      sub,
      roles: ['dentist'],
      azp: 'cliente-estrangeiro',
    });

    // Act
    const act = sut.verify(token);

    // Assert
    await expect(act).rejects.toBeInstanceOf(InvalidTokenError);
    await act.catch((err) => expectNoLeak(token, err));
  });

  it('rejeita token com alg=none', async () => {
    // Arrange
    const sub = randomUUID();
    const sut = buildVerifier();
    const token = await kit.signToken({ sub, roles: ['dentist'], alg: 'none' });

    // Act
    const act = sut.verify(token);

    // Assert
    await expect(act).rejects.toBeInstanceOf(InvalidTokenError);
    await act.catch((err) => expectNoLeak(token, err));
  });

  it('rejeita HS256 assinado tendo a chave pública como segredo (confusão de algoritmo)', async () => {
    // Arrange
    const sub = randomUUID();
    const sut = buildVerifier();
    // A chave pública (RSA) serializada é usada como segredo HMAC: o
    // verificador só aceita RS256 via JWKS, então deve rejeitar.
    const token = await new SignJWT({
      azp: kit.clientId,
      realm_access: { roles: ['dentist'] },
    })
      .setProtectedHeader({ alg: 'HS256', typ: 'JWT' })
      .setIssuer(kit.issuer)
      .setSubject(sub)
      .setIssuedAt()
      .setExpirationTime('1h')
      .sign(new TextEncoder().encode('segredo-qualquer'));

    // Act
    const act = sut.verify(token);

    // Assert
    await expect(act).rejects.toBeInstanceOf(InvalidTokenError);
    await act.catch((err) => expectNoLeak(token, err));
  });

  it('rejeita kid desconhecido e aceita após a chave ser publicada (rotação)', async () => {
    // Arrange: chave nova ainda NÃO publicada
    const sut = buildVerifier();
    const fresh = await generateKeyPair('RS256');
    const freshPub = {
      ...(await exportJWK(fresh.publicKey)),
      kid: `rotated-${randomUUID().slice(0, 8)}`,
      alg: 'RS256',
      use: 'sig',
    };
    const sub = randomUUID();
    const token = await kit.signToken({
      key: fresh.privateKey,
      kid: freshPub.kid,
      sub,
    });

    // Act (antes da publicação)
    await expect(sut.verify(token)).rejects.toBeInstanceOf(InvalidTokenError);

    // Arrange: publica a nova chave ao lado da atual (segundo kid)
    kit.setKeys([kit.publicJwk, freshPub]);

    // Act (depois da publicação)
    const user = await sut.verify(token);

    // Assert
    expect(user.id).toBe(sub);
    expect(user.roles).toEqual([Role.Dentist]);

    // Cleanup: volta a publicar só a chave original
    kit.setKeys([kit.publicJwk]);
  });

  it('aceita token assinado após rotate() (somente a nova chave publicada)', async () => {
    // Arrange
    const { kid } = await kit.rotate();
    const sut = buildVerifier();
    const sub = randomUUID();
    const token = await kit.signToken({ sub, roles: ['dentist'] });

    // Act
    const user = await sut.verify(token);

    // Assert
    expect(user.id).toBe(sub);
    expect(kit.kid).toBe(kid);
  });

  it('rejeita com InvalidTokenError quando o JWKS está fora do ar', async () => {
    // Arrange: derruba o JWKS e usa kid desconhecido (força fetch, sem cache)
    const offlineKit = await createJwtTestKit({});
    const offlineVerifier = new JoseJwksTokenVerifier(
      buildTestConfig({
        KEYCLOAK_ISSUER: offlineKit.issuer,
        KEYCLOAK_JWKS_URI: offlineKit.jwksUri,
        KEYCLOAK_CLIENT_ID: offlineKit.clientId,
        JWKS_CACHE_MAX_AGE_MS: 1000,
        JWKS_COOLDOWN_MS: 0,
        DOWNSTREAM_TIMEOUT_MS: 1000,
      }),
    );
    const fresh = await generateKeyPair('RS256');
    const token = await offlineKit.signToken({
      key: fresh.privateKey,
      kid: `unknown-${randomUUID().slice(0, 8)}`,
      sub: randomUUID(),
    });
    await offlineKit.close();

    // Act
    const act = offlineVerifier.verify(token);

    // Assert
    await expect(act).rejects.toBeInstanceOf(InvalidTokenError);
    await act.catch((err) => expectNoLeak(token, err));
  });

  it('rejeita token malformado / string aleatória', async () => {
    // Arrange
    const sut = buildVerifier();
    const token = `malformado-${randomUUID()}.abc.def`;

    // Act
    const act = sut.verify(token);

    // Assert
    await expect(act).rejects.toBeInstanceOf(InvalidTokenError);
    await act.catch((err) => expectNoLeak(token, err));
  });
});
