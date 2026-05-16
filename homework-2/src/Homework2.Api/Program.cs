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
});

builder.Services.AddScoped<IValidator<CreateTicketRequest>, CreateTicketValidator>();
builder.Services.AddScoped<IValidator<UpdateTicketRequest>, UpdateTicketValidator>();

builder.Services.AddSingleton<ITicketRepository, InMemoryTicketRepository>();
builder.Services.AddScoped<TicketService>();

WebApplication app = builder.Build();

_ = app.MapGet("/health", () => new { status = "ok" })
    .WithName("Health")
    .Produces(200);

app.MapTickets();

app.Run();
