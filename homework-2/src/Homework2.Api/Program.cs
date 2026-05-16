using System.Text.Json;
using System.Text.Json.Serialization;
using Homework2.Bll.Abstractions;
using Homework2.Dal.Repositories;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddSingleton<ITicketRepository, InMemoryTicketRepository>();

WebApplication app = builder.Build();

app.UseHttpsRedirection();

_ = app.MapGet("/health", () => new { status = "ok" })
    .WithName("Health")
    .Produces(200);

app.Run();
