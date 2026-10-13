using System.Data;
using System.Globalization;
using Dentist_service.DentistsServices.Domain;
using Dentist_service.DentistsServices.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Dentist_service.DentistsServices.Api.Controllers;

[ApiController]
[Route("api")]
[Produces("application/json")]
public class AppointmentsController(DentistServicesDbContext db) : ControllerBase
{
    private static readonly TimeZoneInfo ClinicZone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
    private Guid? HeaderGuid(string name) => Request.Headers.TryGetValue(name, out var value) && Guid.TryParse(value, out var id) ? id : null;

    public record WeeklyHoursItem(int DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);
    public record ExceptionRequest(DateOnly Date, bool IsClosed, TimeOnly? StartTime, TimeOnly? EndTime);
    public record CreateAppointmentRequest(Guid ServiceId, string StartAt, string Type, string? InsuranceName, string? InsuranceCardNumber, Guid? PatientId);
    [HttpGet("availability")]
    public async Task<IActionResult> Availability([FromQuery] Guid dentistId, [FromQuery] Guid serviceId, CancellationToken ct)
    {
        var requestingDentistId = HeaderGuid("X-Dentist-Id");
        if (requestingDentistId is not null && requestingDentistId != dentistId)
            return StatusCode(403, new { message = "Dentista só pode consultar a própria agenda." });
        var service = await db.DentistServices.AsNoTracking().FirstOrDefaultAsync(x => x.Id == serviceId && x.DentistId == dentistId, ct);
        if (service is null) return NotFound(new { message = "Serviço não encontrado." });
        if (service.Status != DentistServiceStatus.disponivel || service.DeletedAt != default)
            return BadRequest(new { message = "Serviço indisponível para agendamento." });

        var now = DateTime.UtcNow;
        var end = now.AddDays(30);
        var booked = await db.Appointments.AsNoTracking().Where(x => x.DentistId == dentistId && x.StartAtUtc < end && x.EndAtUtc > now && (x.Status == AppointmentStatus.Pending || x.Status == AppointmentStatus.Confirmed)).ToListAsync(ct);
        var weekly = await db.WeeklyHours.AsNoTracking().Where(x => x.DentistId == dentistId).ToListAsync(ct);
        var exceptions = await db.ScheduleExceptions.AsNoTracking().Where(x => x.DentistId == dentistId && x.Date >= DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(now, ClinicZone)) && x.Date <= DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(end, ClinicZone))).ToDictionaryAsync(x => x.Date, ct);
        var slots = new List<DateTimeOffset>();
        var firstDay = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(now, ClinicZone));
        for (var day = firstDay; day <= DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(end, ClinicZone)); day = day.AddDays(1))
        {
            var weekday = (int)day.DayOfWeek;
            var hours = weekly.FirstOrDefault(x => x.DayOfWeek == weekday);
            var startTime = hours?.StartTime;
            var endTime = hours?.EndTime;
            if (exceptions.TryGetValue(day, out var exception))
            {
                if (exception.IsClosed) continue;
                startTime = exception.StartTime;
                endTime = exception.EndTime;
            }
            if (startTime is null || endTime is null || startTime >= endTime) continue;
            var localStart = day.ToDateTime(startTime.Value);
            var localEnd = day.ToDateTime(endTime.Value);
            for (var cursor = localStart; cursor.AddMinutes(service.DurationMinutes) <= localEnd; cursor = cursor.AddMinutes(service.DurationMinutes))
            {
                if (ClinicZone.IsInvalidTime(cursor) || ClinicZone.IsAmbiguousTime(cursor)) continue;
                var startUtc = TimeZoneInfo.ConvertTimeToUtc(cursor, ClinicZone);
                var endUtc = startUtc.AddMinutes(service.DurationMinutes);
                if (startUtc <= now || booked.Any(x => x.StartAtUtc < endUtc && x.EndAtUtc > startUtc)) continue;
                slots.Add(new DateTimeOffset(startUtc, TimeSpan.Zero));
            }
        }
        return Ok(slots);
    }

    [HttpPut("dentist-schedule/weekly")]
    public async Task<IActionResult> SetWeeklyHours([FromBody] List<WeeklyHoursItem> items, CancellationToken ct)
    {
        var dentistId = HeaderGuid("X-Dentist-Id");
        if (dentistId is null) return BadRequest(new { message = "Header X-Dentist-Id ausente ou inválido." });
        if (items.Any(x => x.DayOfWeek is < 0 or > 6 || x.StartTime >= x.EndTime) || items.Select(x => x.DayOfWeek).Distinct().Count() != items.Count)
            return BadRequest(new { message = "Expediente inválido: dias devem ser únicos e intervalos válidos." });
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.WeeklyHours.Where(x => x.DentistId == dentistId).ExecuteDeleteAsync(ct);
        db.WeeklyHours.AddRange(items.Select(x => new DentistWeeklyHours(dentistId.Value, x.DayOfWeek, x.StartTime, x.EndTime)));
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return Ok(items);
    }

    [HttpGet("dentist-schedule/weekly")]
    public async Task<IActionResult> GetWeeklyHours(CancellationToken ct)
    {
        var dentistId = HeaderGuid("X-Dentist-Id");
        if (dentistId is null) return BadRequest(new { message = "Header X-Dentist-Id ausente ou inválido." });
        return Ok(await db.WeeklyHours.AsNoTracking().Where(x => x.DentistId == dentistId).OrderBy(x => x.DayOfWeek).Select(x => new WeeklyHoursItem(x.DayOfWeek, x.StartTime, x.EndTime)).ToListAsync(ct));
    }

    [HttpPut("dentist-schedule/exceptions")]
    public async Task<IActionResult> SetException([FromBody] ExceptionRequest request, CancellationToken ct)
    {
        var dentistId = HeaderGuid("X-Dentist-Id");
        if (dentistId is null) return BadRequest(new { message = "Header X-Dentist-Id ausente ou inválido." });
        if (request.Date < DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ClinicZone)) || (!request.IsClosed && (request.StartTime is null || request.EndTime is null || request.StartTime >= request.EndTime)))
            return BadRequest(new { message = "Exceção de agenda inválida." });
        var old = await db.ScheduleExceptions.FirstOrDefaultAsync(x => x.DentistId == dentistId && x.Date == request.Date, ct);
        if (old is not null)
        {
            db.ScheduleExceptions.Remove(old);
            await db.SaveChangesAsync(ct);
        }
        db.ScheduleExceptions.Add(new DentistScheduleException(dentistId.Value, request.Date, request.IsClosed, request.StartTime, request.EndTime));
        await db.SaveChangesAsync(ct);
        return Ok(request);
    }

    [HttpGet("dentist-schedule/exceptions")]
    public async Task<IActionResult> GetExceptions(CancellationToken ct)
    {
        var dentistId = HeaderGuid("X-Dentist-Id");
        if (dentistId is null) return BadRequest(new { message = "Header X-Dentist-Id ausente ou inválido." });
        return Ok(await db.ScheduleExceptions.AsNoTracking().Where(x => x.DentistId == dentistId).OrderBy(x => x.Date).Select(x => new ExceptionRequest(x.Date, x.IsClosed, x.StartTime, x.EndTime)).ToListAsync(ct));
    }

    [HttpPost("appointments")]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentRequest request, CancellationToken ct)
    {
        var patientHeader = HeaderGuid("X-Patient-Id");
        var dentistHeader = HeaderGuid("X-Dentist-Id");
        if (patientHeader is null && dentistHeader is null) return BadRequest(new { message = "Identidade do paciente ou dentista ausente." });
        if (patientHeader is not null && request.PatientId is not null) return BadRequest(new { message = "Paciente não pode informar outro paciente." });
        if (!Enum.TryParse<AppointmentType>(request.Type, true, out var type)) return BadRequest(new { message = "Tipo deve ser Private ou Insurance." });
        if (type == AppointmentType.Insurance && (string.IsNullOrWhiteSpace(request.InsuranceName) || string.IsNullOrWhiteSpace(request.InsuranceCardNumber) || request.InsuranceName.Length > 120 || request.InsuranceCardNumber.Length > 80)) return BadRequest(new { message = "Convênio e carteirinha são obrigatórios (máximo de 120 e 80 caracteres)." });
        if (type == AppointmentType.Private && (request.InsuranceName is not null || request.InsuranceCardNumber is not null)) return BadRequest(new { message = "Dados de convênio não se aplicam a consulta particular." });
        var service = await db.DentistServices.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.ServiceId && x.Status == DentistServiceStatus.disponivel && x.DeletedAt == default, ct);
        if (service is null) return NotFound(new { message = "Serviço indisponível ou não encontrado." });
        var patientId = patientHeader ?? request.PatientId;
        if (patientId is null) return BadRequest(new { message = "Dentista deve informar o UUID do paciente." });
        if (dentistHeader is not null && service.DentistId != dentistHeader) return StatusCode(403, new { message = "Serviço não pertence ao dentista autenticado." });
        if (!DateTimeOffset.TryParse(request.StartAt, CultureInfo.InvariantCulture, DateTimeStyles.None, out var startAt) ||
            !(request.StartAt.EndsWith("Z", StringComparison.OrdinalIgnoreCase) || request.StartAt.Contains('+') || request.StartAt.LastIndexOf('-') > 9))
            return BadRequest(new { message = "StartAt deve incluir offset ISO 8601." });
        var start = startAt.UtcDateTime;
        var now = DateTime.UtcNow;
        if (start <= now || start > now.AddDays(30)) return BadRequest(new { message = "Data fora do horizonte de 30 dias." });
        var local = TimeZoneInfo.ConvertTime(startAt, ClinicZone);
        var date = DateOnly.FromDateTime(local.DateTime);
        var exception = await db.ScheduleExceptions.AsNoTracking().FirstOrDefaultAsync(x => x.DentistId == service.DentistId && x.Date == date, ct);
        var hours = await db.WeeklyHours.AsNoTracking().FirstOrDefaultAsync(x => x.DentistId == service.DentistId && x.DayOfWeek == (int)date.DayOfWeek, ct);
        var windowStart = exception is not null ? exception.StartTime : hours?.StartTime;
        var windowEnd = exception is not null ? exception.EndTime : hours?.EndTime;
        var requestedLocalTime = TimeOnly.FromDateTime(local.DateTime);
        var open = (exception is null || !exception.IsClosed) && windowStart is not null && windowEnd is not null && windowStart <= requestedLocalTime && windowEnd >= TimeOnly.FromDateTime(local.DateTime.AddMinutes(service.DurationMinutes));
        var offsetTicks = windowStart is null ? -1 : requestedLocalTime.Ticks - windowStart.Value.Ticks;
        var aligned = offsetTicks >= 0 && offsetTicks % TimeSpan.FromMinutes(service.DurationMinutes).Ticks == 0 && local.Second == 0 && local.Millisecond == 0;
        if (!open || !aligned) return Conflict(new { message = "Horário fora dos horários disponíveis." });
        var end = start.AddMinutes(service.DurationMinutes);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var overlap = await db.Appointments.AnyAsync(x => x.DentistId == service.DentistId && x.Status != AppointmentStatus.Rejected && x.Status != AppointmentStatus.Cancelled && x.StartAtUtc < end && x.EndAtUtc > start, ct);
        if (overlap) return Conflict(new { message = "Horário ocupado." });
        var appointment = new Appointment(service.DentistId, patientId.Value, service.Id, start, end, type, type == AppointmentType.Private ? service.Price : 0, request.InsuranceName, request.InsuranceCardNumber);
        db.Appointments.Add(appointment);
        try { await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); }
        catch (Exception ex) when (ex is DbUpdateException || ex is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure })
        { return Conflict(new { message = "Horário ocupado." }); }
        return Created($"/api/appointments/{appointment.Id}", appointment);
    }

    [HttpGet("appointments")]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var patientId = HeaderGuid("X-Patient-Id"); var dentistId = HeaderGuid("X-Dentist-Id");
        if (patientId is null && dentistId is null) return BadRequest(new { message = "Identidade ausente." });
        var query = db.Appointments.AsNoTracking().AsQueryable();
        if (patientId is not null) query = query.Where(x => x.PatientId == patientId);
        else query = query.Where(x => x.DentistId == dentistId);
        return Ok(await query.OrderBy(x => x.StartAtUtc).ToListAsync(ct));
    }

    [HttpPost("appointments/{id:guid}/confirm")]
    public Task<IActionResult> Confirm(Guid id, CancellationToken ct) => Transition(id, AppointmentStatus.Confirmed, ct);
    [HttpPost("appointments/{id:guid}/reject")]
    public Task<IActionResult> Reject(Guid id, CancellationToken ct) => Transition(id, AppointmentStatus.Rejected, ct);

    private async Task<IActionResult> Transition(Guid id, AppointmentStatus status, CancellationToken ct)
    {
        var dentistId = HeaderGuid("X-Dentist-Id"); if (dentistId is null) return BadRequest(new { message = "Header X-Dentist-Id ausente ou inválido." });
        var item = await db.Appointments.FirstOrDefaultAsync(x => x.Id == id && x.DentistId == dentistId, ct);
        if (item is null) return NotFound(new { message = "Agendamento não encontrado." });
        if (item.Status != AppointmentStatus.Pending) return Conflict(new { message = "Somente pedidos pendentes podem ser confirmados ou rejeitados." });
        if (status == AppointmentStatus.Confirmed)
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var overlap = await db.Appointments.AnyAsync(x => x.Id != id && x.DentistId == dentistId && (x.Status == AppointmentStatus.Pending || x.Status == AppointmentStatus.Confirmed) && x.StartAtUtc < item.EndAtUtc && x.EndAtUtc > item.StartAtUtc, ct);
            if (overlap) return Conflict(new { message = "Horário ocupado." });
            item.SetStatus(status);
            try { await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); }
            catch (Exception ex) when (ex is DbUpdateException || ex is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure })
            { return Conflict(new { message = "Horário ocupado." }); }
        }
        else { item.SetStatus(status); await db.SaveChangesAsync(ct); }
        return Ok(item);
    }

    [HttpPost("appointments/{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var dentistId = HeaderGuid("X-Dentist-Id"); var patientId = HeaderGuid("X-Patient-Id");
        var query = db.Appointments.AsQueryable();
        if (dentistId is not null) query = query.Where(x => x.DentistId == dentistId);
        else if (patientId is not null) query = query.Where(x => x.PatientId == patientId);
        else return BadRequest(new { message = "Identidade ausente." });
        var item = await query.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound(new { message = "Agendamento não encontrado." });
        if (item.Status is AppointmentStatus.Cancelled or AppointmentStatus.Rejected) return Conflict(new { message = "Agendamento já encerrado." });
        if (dentistId is null && item.Status == AppointmentStatus.Confirmed && item.StartAtUtc < DateTime.UtcNow.AddHours(24)) return Conflict(new { message = "Cancelamento de consulta confirmada exige 24 horas de antecedência." });
        item.SetStatus(AppointmentStatus.Cancelled); await db.SaveChangesAsync(ct); return Ok(item);
    }
}
