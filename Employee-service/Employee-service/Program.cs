var builder = WebApplication.CreateBuilder(args);

// ──────────────────────────────────────────────
// Infraestrutura: EF Core + PostgreSQL
// (registrada na etapa de infraestrutura)
// ──────────────────────────────────────────────

// ──────────────────────────────────────────────
// Portas → Adaptadores (Hexagonal)
// (registradas conforme as camadas forem implementadas)
// ──────────────────────────────────────────────

// ──────────────────────────────────────────────
// API
// ──────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Permite enviar cargo/status como string ("recepcionista", "ativo") em vez de número inteiro
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
