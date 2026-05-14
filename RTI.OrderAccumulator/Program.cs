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
    var connectionString = builder.Configuration.GetConnectionString("TradingDatabase") ?? "Data Source=orderAccumulator.db";
    options.UseSqlite(connectionString);
});

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IExposureRepository, ExposureRepository>();

builder.Services.AddSingleton<ExposureService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TradingDbContext>>();
    using var db = factory.CreateDbContext();
    db.Database.EnsureCreated();
}

var exposureService = app.Services.GetRequiredService<ExposureService>();

app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowOrderGenerator");
app.MapControllers();

var fixTask = Task.Run(() =>
{
    Console.WriteLine("RTI Order Accumulator - FIX Server");
    var acceptor = new FixAcceptor(exposureService);
    acceptor.Start();
});

app.Run();

