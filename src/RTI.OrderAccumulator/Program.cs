using Microsoft.EntityFrameworkCore;
using RTI.OrderAccumulator.Data;
using RTI.OrderAccumulator.Fix;
using RTI.OrderAccumulator.Repositories;
using RTI.OrderAccumulator.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowOrderGenerator", policy =>
    {
        policy.WithOrigins("http://localhost:5000", "http://localhost:5071", "http://localhost:5001", "http://localhost:5002", "https://localhost:5000", "https://localhost:5001", "https://localhost:5002", "https://localhost:5003")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContextFactory<TradingDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("TradingDatabase")
        ?? "Host=localhost;Port=5432;Database=orderAccumulator;Username=postgres;Password=postgres";
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    });
});

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IExposureRepository, ExposureRepository>();

builder.Services.AddSingleton<ExposureService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TradingDbContext>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    const int maxRetries = 10;
    var retryDelay = TimeSpan.FromSeconds(2);
    for (var attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            using var db = factory.CreateDbContext();
            db.Database.EnsureCreated();
            break;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Postgres database unavailable on attempt {Attempt}/{MaxRetries}. Retrying in {Delay}s...", attempt, maxRetries, retryDelay.TotalSeconds);
            if (attempt == maxRetries)
            {
                logger.LogError(ex, "Failed to initialize the database after {MaxRetries} attempts.", maxRetries);
                throw;
            }
            await Task.Delay(retryDelay);
        }
    }
}

var exposureService = app.Services.GetRequiredService<ExposureService>();
var acceptorLogger = app.Services.GetRequiredService<ILogger<FixAcceptor>>();

app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowOrderGenerator");
app.MapControllers();

var fixTask = Task.Run(() =>
{
    acceptorLogger.LogInformation("RTI Order Accumulator - FIX Server starting");
    var acceptor = new FixAcceptor(exposureService, acceptorLogger);
    acceptor.Start();
});

app.Run();

