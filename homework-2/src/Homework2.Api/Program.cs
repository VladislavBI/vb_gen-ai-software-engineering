using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Homework2.Api.Endpoints;
using Homework2.Api.Models;
using Homework2.Api.Validators;
using Homework2.Bll.Abstractions;
using Homework2.Bll.Services;
using Homework2.Dal.Repositories;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
});

builder.Services.AddScoped<IValidator<CreateTicketRequest>, CreateTicketValidator>();
builder.Services.AddScoped<IValidator<UpdateTicketRequest>, UpdateTicketValidator>();

builder.Services.AddSingleton<ITicketRepository, InMemoryTicketRepository>();
builder.Services.AddScoped<TicketService>();
builder.Services.AddScoped<TicketImportService>();
builder.Services.AddScoped<TicketClassifier>();

WebApplication app = builder.Build();

_ = app.MapGet("/health", () => new { status = "ok" })
    .WithName("Health")
    .Produces(200);

app.MapTicketsImport();
app.MapClassify();
app.MapTickets();

app.Run();

#pragma warning disable CA1515, CS1591
/// <summary>Program class for testing purposes.</summary>
public partial class Program;
#pragma warning restore CA1515, CS1591
