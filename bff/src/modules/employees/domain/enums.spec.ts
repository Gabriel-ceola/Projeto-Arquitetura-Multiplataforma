import { EmployeeRole } from './employee-role.enum';
import { EmployeeStatus } from './employee-status.enum';

describe('EmployeeRole', () => {
  it('contém exatamente os valores aceitos pelo Employee-service', () => {
    // Arrange
    const valores = Object.values(EmployeeRole);

    // Act
    const ordenados = [...valores].sort();

    // Assert
    expect(ordenados).toEqual(['administrativo', 'auxiliar', 'recepcionista']);
  });
});

describe('EmployeeStatus', () => {
  it('contém exatamente os valores aceitos pelo Employee-service', () => {
    // Arrange
    const valores = Object.values(EmployeeStatus);

    // Act
    const ordenados = [...valores].sort();

    // Assert
    expect(ordenados).toEqual(['ativo', 'ferias', 'inativo']);
  });
});
