import { InvalidTokenError } from '../domain/auth-errors';
import { AuthenticateUseCase } from './authenticate.use-case';
import { FakeTokenVerifier } from '../../../../test/support/fakes/fake-token-verifier';
import { anAuthenticatedUser } from '../../../../test/support/builders/authenticated-user.builder';

describe('AuthenticateUseCase', () => {
  it('chama verify e devolve o usuário quando o cabeçalho é válido', async () => {
    // Arrange
    const esperado = anAuthenticatedUser();
    const verifier = new FakeTokenVerifier(esperado);
    const sut = new AuthenticateUseCase(verifier);

    // Act
    const resultado = await sut.execute({
      authorizationHeader: 'Bearer token-123',
    });

    // Assert
    expect(resultado).toEqual(esperado);
    expect(verifier.verifiedTokens).toEqual(['token-123']);
  });

  it('lança InvalidTokenError sem chamar verify quando o cabeçalho é undefined', async () => {
    // Arrange
    const verifier = new FakeTokenVerifier();
    const sut = new AuthenticateUseCase(verifier);

    // Act
    const promessa = sut.execute({ authorizationHeader: undefined });

    // Assert
    await expect(promessa).rejects.toBeInstanceOf(InvalidTokenError);
    expect(verifier.verifiedTokens).toHaveLength(0);
  });

  it('lança InvalidTokenError sem chamar verify quando o cabeçalho é vazio', async () => {
    // Arrange
    const verifier = new FakeTokenVerifier();
    const sut = new AuthenticateUseCase(verifier);

    // Act
    const promessa = sut.execute({ authorizationHeader: '' });

    // Assert
    await expect(promessa).rejects.toBeInstanceOf(InvalidTokenError);
    expect(verifier.verifiedTokens).toHaveLength(0);
  });

  it('lança InvalidTokenError quando o esquema não é Bearer', async () => {
    // Arrange
    const verifier = new FakeTokenVerifier();
    const sut = new AuthenticateUseCase(verifier);

    // Act
    const promessa = sut.execute({ authorizationHeader: 'Basic xxx' });

    // Assert
    await expect(promessa).rejects.toBeInstanceOf(InvalidTokenError);
    expect(verifier.verifiedTokens).toHaveLength(0);
  });

  it('lança InvalidTokenError quando Bearer vem sem token', async () => {
    // Arrange
    const verifier = new FakeTokenVerifier();
    const sut = new AuthenticateUseCase(verifier);

    // Act
    const promessa = sut.execute({ authorizationHeader: 'Bearer' });

    // Assert
    await expect(promessa).rejects.toBeInstanceOf(InvalidTokenError);
    expect(verifier.verifiedTokens).toHaveLength(0);
  });

  it('aceita o esquema bearer em minúsculo', async () => {
    // Arrange
    const esperado = anAuthenticatedUser();
    const verifier = new FakeTokenVerifier(esperado);
    const sut = new AuthenticateUseCase(verifier);

    // Act
    const resultado = await sut.execute({
      authorizationHeader: 'bearer token-abc',
    });

    // Assert
    expect(resultado).toEqual(esperado);
    expect(verifier.verifiedTokens).toEqual(['token-abc']);
  });

  it('propaga o erro quando verify lança', async () => {
    // Arrange
    const verifier = new FakeTokenVerifier();
    verifier.failNext();
    const sut = new AuthenticateUseCase(verifier);

    // Act
    const promessa = sut.execute({ authorizationHeader: 'Bearer token-x' });

    // Assert
    await expect(promessa).rejects.toBeInstanceOf(InvalidTokenError);
  });
});
