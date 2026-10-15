namespace Messaging.Contracts;

public static class Topics
{
    public const string Appointments = "appointments";
    public const string Payments = "payments";
    public const string HealthInsurance = "health-insurance";
    public const string DeadLetterSuffix = ".dlq";

    public static string DeadLetterOf(string topic) => topic + DeadLetterSuffix;
}
