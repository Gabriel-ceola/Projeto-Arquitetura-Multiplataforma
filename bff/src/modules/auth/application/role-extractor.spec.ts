import { Role } from '../domain/role.enum';
import { extractRoles } from './role-extractor';

describe('extractRoles', () => {
  it('extrai papéis da fonte realm quando presentes em realm_access', () => {
    // Arrange
    const claims = { realm_access: { roles: ['dentist'] } };

    // Act
    const resultado = extractRoles(claims, {
      source: 'realm',
      clientId: 'bff',
    });

    // Assert
    expect(resultado).toEqual([Role.Dentist]);
  });

  it('extrai papéis da fonte client quando presentes em resource_access', () => {
    // Arrange
    const claims = {
      resource_access: { 'meu-client': { roles: ['employ'] } },
    };

    // Act
    const resultado = extractRoles(claims, {
      source: 'client',
      clientId: 'meu-client',
    });

    // Assert
    expect(resultado).toEqual([Role.Employ]);
  });

  it('retorna lista vazia quando a fonte é realm mas os papéis estão só no client', () => {
    // Arrange
    const claims = {
      resource_access: { 'meu-client': { roles: ['dentist'] } },
    };

    // Act
    const resultado = extractRoles(claims, {
      source: 'realm',
      clientId: 'meu-client',
    });

    // Assert
    expect(resultado).toEqual([]);
  });

  it('retorna lista vazia quando realm_access está ausente', () => {
    // Arrange
    const claims = {};

    // Act
    const resultado = extractRoles(claims, {
      source: 'realm',
      clientId: 'bff',
    });

    // Assert
    expect(resultado).toEqual([]);
  });

  it('ignora papéis desconhecidos', () => {
    // Arrange
    const claims = {
      realm_access: { roles: ['dentist', 'admin-desconhecido'] },
    };

    // Act
    const resultado = extractRoles(claims, {
      source: 'realm',
      clientId: 'bff',
    });

    // Assert
    expect(resultado).toEqual([Role.Dentist]);
  });

  it('remove papéis duplicados', () => {
    // Arrange
    const claims = {
      realm_access: { roles: ['dentist', 'dentist', 'employ'] },
    };

    // Act
    const resultado = extractRoles(claims, {
      source: 'realm',
      clientId: 'bff',
    });

    // Assert
    expect(resultado).toEqual([Role.Dentist, Role.Employ]);
  });

  it('retorna lista vazia quando papéis não são um array', () => {
    // Arrange
    const claims = { realm_access: { roles: 'dentist' } };

    // Act
    const resultado = extractRoles(claims, {
      source: 'realm',
      clientId: 'bff',
    });

    // Assert
    expect(resultado).toEqual([]);
  });

  it('extrai vários papéis válidos', () => {
    // Arrange
    const claims = {
      realm_access: { roles: ['employ', 'dentist', 'patient'] },
    };

    // Act
    const resultado = extractRoles(claims, {
      source: 'realm',
      clientId: 'bff',
    });

    // Assert
    expect(resultado).toEqual([Role.Employ, Role.Dentist, Role.Patient]);
  });

  it('não reconhece papel com maiúscula diferente do valor exato', () => {
    // Arrange
    const claims = { realm_access: { roles: ['Dentist'] } };

    // Act
    const resultado = extractRoles(claims, {
      source: 'realm',
      clientId: 'bff',
    });

    // Assert
    expect(resultado).toEqual([]);
  });
});
