using Patient_service.Patients.Domain;

namespace Patient_service.Patients.Application.DTOs;

// ──────────────────────────────────────────────
// Requests
// ──────────────────────────────────────────────

/// <summary>
/// Payload para cadastrar um novo paciente.
/// </summary>
public record CreatePatientRequest(
    string Name,
    string Cpf,
    string Email,
    string Phone,
    DateOnly BirthDate,
    PatientStatus Status,
    string? InsuranceName = null,
    string? InsuranceCardNumber = null
);

/// <summary>
/// Payload para editar um paciente existente.
/// O CPF não faz parte da edição: ele identifica o paciente e é imutável.
/// Convênio omitido (null) mantém o atual; string vazia remove.
/// </summary>
public record UpdatePatientRequest(
    string Name,
    string Email,
    string Phone,
    DateOnly BirthDate,
    PatientStatus Status,
    string? InsuranceName = null,
    string? InsuranceCardNumber = null
);

// ──────────────────────────────────────────────
// Response
// ──────────────────────────────────────────────

/// <summary>
/// Dados retornados ao cliente após operações de leitura/escrita.
/// </summary>
public record PatientResponse(
    Guid Id,
    string Name,
    string Cpf,
    string Email,
    string Phone,
    DateOnly BirthDate,
    string? InsuranceName,
    string? InsuranceCardNumber,
    PatientStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
