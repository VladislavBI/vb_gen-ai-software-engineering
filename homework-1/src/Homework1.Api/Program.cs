using System.Collections.Concurrent;
using Homework1.Api.Endpoints;
using Homework1.Bll.Abstractions;
using Homework1.Bll.Services;
using Homework1.Dal.Repositories;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<System.Text.Json.JsonSerializerOptions>(options =>
    options.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);

builder.Services.AddSingleton(new ConcurrentDictionary<Guid, InMemoryTransactionRepository.TransactionEntity>());
builder.Services.AddScoped<InMemoryTransactionRepository>();
builder.Services.AddScoped<ITransactionRepository>(sp =>
    sp.GetRequiredService<InMemoryTransactionRepository>());
builder.Services.AddScoped<TransactionService>();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapTransactions();
app.MapAccounts();

app.Run();
