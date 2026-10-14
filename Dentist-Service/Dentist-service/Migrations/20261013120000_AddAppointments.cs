using Dentist_service.DentistsServices.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dentist_service.Migrations;

[DbContext(typeof(DentistServicesDbContext))]
[Migration("20261013120000_AddAppointments")]
public partial class AddAppointments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "duration_minutes", table: "dentist_services", type: "integer", nullable: false, defaultValue: 30);

        migrationBuilder.CreateTable(name: "dentist_weekly_hours", columns: table => new
        {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            dentist_id = table.Column<Guid>(type: "uuid", nullable: false),
            day_of_week = table.Column<int>(type: "integer", nullable: false),
            start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
            end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_dentist_weekly_hours", x => x.id));

        migrationBuilder.CreateTable(name: "dentist_schedule_exceptions", columns: table => new
        {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            dentist_id = table.Column<Guid>(type: "uuid", nullable: false),
            date = table.Column<DateOnly>(type: "date", nullable: false),
            is_closed = table.Column<bool>(type: "boolean", nullable: false),
            start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
            end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true)
        }, constraints: table => table.PrimaryKey("PK_dentist_schedule_exceptions", x => x.id));

        migrationBuilder.CreateTable(name: "appointments", columns: table => new
        {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            dentist_id = table.Column<Guid>(type: "uuid", nullable: false),
            patient_id = table.Column<Guid>(type: "uuid", nullable: false),
            service_id = table.Column<Guid>(type: "uuid", nullable: false),
            start_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            end_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            type = table.Column<string>(type: "text", nullable: false),
            status = table.Column<string>(type: "text", nullable: false),
            price_cents = table.Column<int>(type: "integer", nullable: false),
            insurance_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
            insurance_card_number = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
            created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_appointments", x => x.id));

        migrationBuilder.CreateIndex("ix_dentist_weekly_hours_dentist_day", "dentist_weekly_hours", new[] { "dentist_id", "day_of_week" }, unique: true);
        migrationBuilder.CreateIndex("ix_dentist_schedule_exceptions_dentist_date", "dentist_schedule_exceptions", new[] { "dentist_id", "date" }, unique: true);
        migrationBuilder.CreateIndex("ix_appointments_dentist_interval", "appointments", new[] { "dentist_id", "start_at_utc", "end_at_utc" });
        migrationBuilder.CreateIndex("ix_appointments_patient_start", "appointments", new[] { "patient_id", "start_at_utc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("appointments");
        migrationBuilder.DropTable("dentist_schedule_exceptions");
        migrationBuilder.DropTable("dentist_weekly_hours");
        migrationBuilder.DropColumn(name: "duration_minutes", table: "dentist_services");
    }
}
