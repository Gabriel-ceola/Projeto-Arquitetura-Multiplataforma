using FluentAssertions;
using Messaging.Contracts;
using Messaging.Contracts.Appointments;
using Messaging.Contracts.HealthInsurance;
using Messaging.Contracts.Payments;
using Messaging.Tests.Helpers;

namespace Messaging.Tests.Contracts;

public class ContractsTests
{
    [Fact]
    public void AppointmentEvents_ShouldTargetAppointmentsTopic()
    {
        AppointmentRequested.Topic.Should().Be(Topics.Appointments);
        AppointmentConfirmed.Topic.Should().Be(Topics.Appointments);
        AppointmentRejected.Topic.Should().Be(Topics.Appointments);
        AppointmentCancelled.Topic.Should().Be(Topics.Appointments);
    }

    [Fact]
    public void PaymentEvents_ShouldTargetPaymentsTopic()
    {
        PaymentApproved.Topic.Should().Be(Topics.Payments);
        PaymentFailed.Topic.Should().Be(Topics.Payments);
    }

    [Fact]
    public void HealthInsuranceEvents_ShouldTargetHealthInsuranceTopic()
    {
        InsuranceAuthorized.Topic.Should().Be(Topics.HealthInsurance);
        InsuranceDenied.Topic.Should().Be(Topics.HealthInsurance);
    }

    [Fact]
    public void EventTypes_ShouldBeUnique()
    {
        string[] eventTypes =
        [
            AppointmentRequested.EventType, AppointmentConfirmed.EventType, AppointmentRejected.EventType,
            AppointmentCancelled.EventType, PaymentApproved.EventType, PaymentFailed.EventType,
            InsuranceAuthorized.EventType, InsuranceDenied.EventType
        ];

        eventTypes.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Key_ShouldBeTheAppointmentIdForEveryContract()
    {
        var appointmentId = Guid.NewGuid();
        var expected = appointmentId.ToString();

        new AppointmentConfirmed(EventsFactory.Snapshot(appointmentId)).Key.Should().Be(expected);
        new AppointmentCancelled(EventsFactory.Snapshot(appointmentId), CancelledBy.Dentist).Key.Should().Be(expected);
        new PaymentApproved(Guid.NewGuid(), appointmentId, 15000, DateTime.UtcNow).Key.Should().Be(expected);
        new PaymentFailed(Guid.NewGuid(), appointmentId, "Cartão recusado").Key.Should().Be(expected);
        new InsuranceAuthorized(Guid.NewGuid(), appointmentId, "Plano X").Key.Should().Be(expected);
        new InsuranceDenied(Guid.NewGuid(), appointmentId, "Sem cobertura").Key.Should().Be(expected);
    }

    [Fact]
    public void DeadLetterOf_ShouldAppendDlqSuffix()
    {
        Topics.DeadLetterOf(Topics.Payments).Should().Be("payments.dlq");
    }
}
