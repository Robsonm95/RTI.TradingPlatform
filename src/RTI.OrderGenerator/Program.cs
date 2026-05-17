using System.Text.Json.Serialization;
using RTI.OrderGenerator.Fix;
using RTI.OrderGenerator.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });
    
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();


builder.Services.AddSingleton<ExecutionReportTracker>();
builder.Services.AddSingleton<OrderFixService>();

var app = builder.Build();

var tracker =
    app.Services.GetRequiredService<
        ExecutionReportTracker>();
var initiatorLogger = app.Services.GetRequiredService<ILogger<FixInitiator>>();
var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();

var initiator = new FixInitiator(tracker, initiatorLogger, loggerFactory);

initiator.Start();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
