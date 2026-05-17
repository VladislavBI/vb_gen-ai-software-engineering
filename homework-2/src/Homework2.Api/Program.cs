using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Homework2.Api.Endpoints;
using Homework2.Api.Models;
using Homework2.Api.Validators;
using Homework2.Bll.Abstractions;
using Homework2.Bll.Services;
using Homework2.Dal.Repositories;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "Homework 2 – Support Tickets API";
        document.Info.Version = "v1";
        document.Info.Description = "CRUD, bulk import, and AI-assisted classification of support tickets.";
        return Task.CompletedTask;
    }));


builder.Services.AddScoped<IValidator<CreateTicketRequest>, CreateTicketValidator>();
builder.Services.AddScoped<IValidator<UpdateTicketRequest>, UpdateTicketValidator>();

builder.Services.AddSingleton<ITicketRepository, InMemoryTicketRepository>();
builder.Services.AddScoped<TicketService>();
builder.Services.AddScoped<TicketImportService>();
builder.Services.AddScoped<TicketClassifier>();

WebApplication app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

_ = app.MapGet("/health", () => new { status = "ok" })
    .WithName("Health")
    .WithTags("Health")
    .Produces(200);

app.MapTicketsImport();
app.MapClassify();
app.MapTickets();

app.Run();
