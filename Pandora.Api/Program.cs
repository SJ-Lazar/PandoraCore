using Pandora.Core.Features.AuditTrail;
using Pandora.Core.Features.Export;
using Pandora.Core.Features.Logging;
using Pandora.Core.Features.Users;
using Pandora.Core.Features.Workflow;
using Pandora.Core.Features.WorkItem;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseCoreSerilog();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddAuditTrail();
builder.Services.AddExport();
builder.Services.AddUsers();
builder.Services.AddSingleton<IWorkItemService, InMemoryWorkItemService>();
builder.Services.AddSingleton<InMemoryWorkflowService>();

var app = builder.Build();

app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
