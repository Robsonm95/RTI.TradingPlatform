using RTI.OrderGenerator.Fix;
using RTI.OrderGenerator.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
