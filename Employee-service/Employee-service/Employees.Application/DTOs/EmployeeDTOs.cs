using Employee_service.Employees.Domain;

namespace Employee_service.Employees.Application.DTOs;

// ──────────────────────────────────────────────
// Requests
// ──────────────────────────────────────────────

/// <summary>
/// Payload para cadastrar um novo funcionário.
/// </summary>
public record CreateEmployeeRequest(
    string Name,
    string Cpf,
    string Email,
    string Phone,
    EmployeeRole Role,
    EmployeeStatus Status
);

/// <summary>
/// Payload para editar um funcionário existente.
/// O CPF não faz parte da edição: ele identifica o funcionário e é imutável.
/// </summary>
public record UpdateEmployeeRequest(
    string Name,
    string Email,
    string Phone,
    EmployeeRole Role,
    EmployeeStatus Status
);

// ──────────────────────────────────────────────
// Response
// ──────────────────────────────────────────────

/// <summary>
/// Dados retornados ao cliente após operações de leitura/escrita.
/// </summary>
public record EmployeeResponse(
    Guid Id,
    string Name,
    string Cpf,
    string Email,
    string Phone,
    EmployeeRole Role,
    EmployeeStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
