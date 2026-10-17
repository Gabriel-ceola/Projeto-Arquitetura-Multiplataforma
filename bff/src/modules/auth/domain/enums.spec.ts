import { Role } from './role.enum';

describe('Role', () => {
  it('expõe o valor exato para Employ', () => {
    // Arrange
    const valor = Role.Employ;

    // Act
    const resultado = valor as string;

    // Assert
    expect(resultado).toBe('employ');
  });

  it('expõe o valor exato para Dentist', () => {
    // Arrange
    const valor = Role.Dentist;

    // Act
    const resultado = valor as string;

    // Assert
    expect(resultado).toBe('dentist');
  });

  it('expõe o valor exato para Patient', () => {
    // Arrange
    const valor = Role.Patient;

    // Act
    const resultado = valor as string;

    // Assert
    expect(resultado).toBe('patient');
  });

  it('contém exatamente os três papéis esperados', () => {
    // Arrange
    const valores = Object.values(Role);

    // Act
    const ordenados = [...valores].sort();

    // Assert
    expect(ordenados).toEqual(['dentist', 'employ', 'patient']);
  });
});
