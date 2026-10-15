using System.Text.Json;
using Dentist_service.DentistsServices.Domain;
using Dentist_service.DentistsServices.Infrastructure.Events;
using FluentAssertions;
using Messaging.Contracts;
using Messaging.Contracts.Appointments;
using Messaging.Outbox;
using Moq;

namespace Dentist_service.Tests.Infrastructure;

public class AppointmentEventPublisherTests
{
    private readonly Mock<IOutbox>            _outboxMock;
    private readonly AppointmentEventPublisher _publisher;

    public AppointmentEventPublisherTests()
    {
        _outboxMock = new Mock<IOutbox>();
        _publisher  = new AppointmentEventPublisher(_outboxMock.Object);
    }

    [Fact]
    public void Requested_WhenAppointmentIsCreated_ShouldEnqueueAppointmentRequestedWithSnapshot()
    {
        var appointment = CreateAppointment();
        AppointmentRequested? captured = null;
        _outboxMock
            .Setup(o => o.Enqueue(It.IsAny<AppointmentRequested>(), It.IsAny<string?>()))
            .Callback<AppointmentRequested, string?>((e, _) => captured = e);

        _publisher.Requested(appointment);

        captured.Should().NotBeNull();
        captured!.Key.Should().Be(appointment.Id.ToString());
        captured.Appointment.Should().Be(new AppointmentSnapshot(
            appointment.Id,
            appointment.DentistId,
            appointment.PatientId,
            appointment.ServiceId,
            appointment.StartAtUtc,
            appointment.EndAtUtc,
            "Private",
            "Pending",
            15000,
            null));
    }

    [Fact]
    public void Confirmed_WhenAppointmentIsConfirmed_ShouldEnqueueAppointmentConfirmed()
    {
        var appointment = CreateAppointment();
        appointment.SetStatus(AppointmentStatus.Confirmed);

        _publisher.Confirmed(appointment);

        _outboxMock.Verify(o => o.Enqueue(
            It.Is<AppointmentConfirmed>(e => e.Appointment.AppointmentId == appointment.Id && e.Appointment.Status == "Confirmed"),
            null), Times.Once);
    }

    [Fact]
    public void Rejected_WhenAppointmentIsRejected_ShouldEnqueueAppointmentRejected()
    {
        var appointment = CreateAppointment();
        appointment.SetStatus(AppointmentStatus.Rejected);

        _publisher.Rejected(appointment);

        _outboxMock.Verify(o => o.Enqueue(
            It.Is<AppointmentRejected>(e => e.Appointment.AppointmentId == appointment.Id && e.Appointment.Status == "Rejected"),
            null), Times.Once);
    }

    [Theory]
    [InlineData(AppointmentActor.Dentist, CancelledBy.Dentist)]
    [InlineData(AppointmentActor.Patient, CancelledBy.Patient)]
    public void Cancelled_WhenAppointmentIsCancelled_ShouldEnqueueWhoCancelled(AppointmentActor actor, CancelledBy expected)
    {
        var appointment = CreateAppointment();
        appointment.SetStatus(AppointmentStatus.Cancelled);

        _publisher.Cancelled(appointment, actor);

        _outboxMock.Verify(o => o.Enqueue(
            It.Is<AppointmentCancelled>(e => e.CancelledBy == expected && e.Appointment.Status == "Cancelled"),
            null), Times.Once);
    }

    [Fact]
    public void Requested_WhenAppointmentUsesInsurance_ShouldNotExposeInsuranceCardNumber()
    {
        var appointment = CreateAppointment(AppointmentType.Insurance, "Plano Sorriso", "CARD-123456");
        AppointmentRequested? captured = null;
        _outboxMock
            .Setup(o => o.Enqueue(It.IsAny<AppointmentRequested>(), It.IsAny<string?>()))
            .Callback<AppointmentRequested, string?>((e, _) => captured = e);

        _publisher.Requested(appointment);

        var json = JsonSerializer.Serialize(captured, MessagingJson.Options);
        captured!.Appointment.InsuranceName.Should().Be("Plano Sorriso");
        json.Should().NotContain("CARD-123456");
    }

    private static Appointment CreateAppointment(
        AppointmentType type = AppointmentType.Private,
        string? insuranceName = null,
        string? insuranceCardNumber = null)
    {
        var start = new DateTime(2026, 10, 20, 13, 0, 0, DateTimeKind.Utc);
        return new Appointment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), start, start.AddMinutes(30),
            type, type == AppointmentType.Private ? 15000 : 0, insuranceName, insuranceCardNumber);
    }
}
