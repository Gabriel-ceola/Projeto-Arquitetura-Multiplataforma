using Patient_service.Patients.Application.UseCases;
using Patient_service.Patients.Domain.Ports;
using Patient_service.Patients.Domain.Validators;
using Patient_service.Patients.Infrastructure.Data;
using Patient_service.Patients.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ──────────────────────────────────────────────
// Infraestrutura: EF Core + PostgreSQL
// ──────────────────────────────────────────────
builder.Services.AddDbContext<PatientDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ──────────────────────────────────────────────
// Portas → Adaptadores (Hexagonal)
// ──────────────────────────────────────────────

// Porta de saída: repositório
builder.Services.AddScoped<IPatientRepository, PatientRepository>();

// Porta de entrada: validação
builder.Services.AddScoped<IPatientValidator, PatientValidator>();

// Porta de entrada: caso de uso
builder.Services.AddScoped<IPatientUseCase, PatientUseCase>();

// ──────────────────────────────────────────────
// API
// ──────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Permite enviar o status como string ("ativo") em vez de número inteiro
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
