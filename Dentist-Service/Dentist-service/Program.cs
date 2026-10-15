using Dentist_service.DentistsServices.Application.UseCases;
using Dentist_service.DentistsServices.Domain.Ports;
using Dentist_service.DentistsServices.Domain.Validators;
using Dentist_service.DentistsServices.Infrastructure.Data;
using Dentist_service.DentistsServices.Infrastructure.Events;
using Dentist_service.DentistsServices.Infrastructure.Repositories;
using Messaging;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ──────────────────────────────────────────────
// Infraestrutura: EF Core + PostgreSQL
// ──────────────────────────────────────────────
builder.Services.AddDbContext<DentistServicesDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddKafkaMessaging(builder.Configuration)
    .AddOutbox<DentistServicesDbContext>()
    .AddConsumer();

// ──────────────────────────────────────────────
// Portas → Adaptadores (Hexagonal)
// ──────────────────────────────────────────────

// Porta de saída: repositório
builder.Services.AddScoped<IDentistServicesRepository, DentistServicesRepository>();

// Porta de entrada: validação
builder.Services.AddScoped<IDentistServicesValidator, DentistServicesValidator>();

// Porta de entrada: caso de uso
builder.Services.AddScoped<IDentistServicesUseCase, DentistServicesUseCase>();

builder.Services.AddScoped<IAppointmentEventPublisher, AppointmentEventPublisher>();

// ──────────────────────────────────────────────
// API
// ──────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Permite enviar o status como string ("disponivel") em vez de número inteiro
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
