using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Services;
using PaymentService.Domain.Ports;
using PaymentService.Infrastructure.Data;
using PaymentService.Infrastructure.Messaging;
using PaymentService.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPaymentRepository, PostgresPaymentRepository>();
builder.Services.AddSingleton<IPaymentEventPublisher, LoggingPaymentEventPublisher>();
builder.Services.AddScoped<IPaymentService, PaymentService.Application.Services.PaymentService>();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();

}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
