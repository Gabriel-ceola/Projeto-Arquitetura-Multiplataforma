import { DentistServiceStatus } from './dentist-service-status.enum';

describe('DentistServiceStatus', () => {
  it('expõe o valor exato para Disponivel', () => {
    // Arrange
    const valor = DentistServiceStatus.Disponivel;

    // Act
    const resultado = valor as string;

    // Assert
    expect(resultado).toBe('disponivel');
  });

  it('expõe o valor exato para Indisponivel', () => {
    // Arrange
    const valor = DentistServiceStatus.Indisponivel;

    // Act
    const resultado = valor as string;

    // Assert
    expect(resultado).toBe('indisponivel');
  });

  it('expõe o valor exato para EmBreve', () => {
    // Arrange
    const valor = DentistServiceStatus.EmBreve;

    // Act
    const resultado = valor as string;

    // Assert
    expect(resultado).toBe('emBreve');
  });

  it('contém exatamente os três status esperados', () => {
    // Arrange
    const valores = Object.values(DentistServiceStatus);

    // Act
    const ordenados = [...valores].sort();

    // Assert
    expect(ordenados).toEqual(['disponivel', 'emBreve', 'indisponivel']);
  });
});
