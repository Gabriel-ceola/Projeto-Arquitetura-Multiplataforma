using Dentist_service.DentistsServices.Domain;
using FluentAssertions;

namespace Dentist_service.Tests.Domain;

public class AppointmentTests
{
    [Fact]
    public void NewAppointmentStartsPendingAndBlocksItsTime()
    {
        var item = new Appointment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(2).AddMinutes(45), AppointmentType.Private, 12500, null, null);

        item.Status.Should().Be(AppointmentStatus.Pending);
        item.PriceCents.Should().Be(12500);
        item.BlocksTime.Should().BeTrue();
    }

    [Theory]
    [InlineData(AppointmentStatus.Confirmed, true)]
    [InlineData(AppointmentStatus.Rejected, false)]
    [InlineData(AppointmentStatus.Cancelled, false)]
    public void OnlyPendingAndConfirmedAppointmentsBlockTime(AppointmentStatus status, bool blocks)
    {
        var item = new Appointment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(2).AddMinutes(30), AppointmentType.Insurance, 0, "Plano", "12345");
        item.SetStatus(status);

        item.BlocksTime.Should().Be(blocks);
        item.InsuranceName.Should().Be("Plano");
        item.InsuranceCardNumber.Should().Be("12345");
    }
}
