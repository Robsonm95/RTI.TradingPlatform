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

var initiator = new FixInitiator(tracker);

initiator.Start();

app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
