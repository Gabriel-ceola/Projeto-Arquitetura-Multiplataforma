using Dentist_service.DentistsServices.Domain;

namespace Dentist_service.DentistsServices.Application.DTOs;

// ──────────────────────────────────────────────
// Requests
// ──────────────────────────────────────────────

/// <summary>
/// Payload para criar um novo serviço odontológico.
/// </summary>
public record CreateDentistServicesRequest(
    string Name,
    Guid DentistId,
    int Price,
    bool IsPeriodic,
    DentistServiceStatus Status,
    int DurationMinutes = 30
);

/// <summary>
/// Payload para atualizar um serviço odontológico existente.
/// </summary>
public record UpdateDentistServicesRequest(
    string Name,
    int Price,
    bool IsPeriodic,
    DentistServiceStatus Status,
    int DurationMinutes = 30
);

// ──────────────────────────────────────────────
// Response
// ──────────────────────────────────────────────

/// <summary>
/// Dados retornados ao cliente após operações de leitura/escrita.
/// </summary>
public record DentistServicesResponse(
    Guid Id,
    string Name,
    Guid DentistId,
    int Price,
    bool IsPeriodic,
    DentistServiceStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int DurationMinutes = 30
);
