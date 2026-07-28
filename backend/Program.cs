using System.Net.Http.Headers;
using backend.Data;
using backend.Endpoints;
using backend.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Default"),
        npgsqlOptions => npgsqlOptions.UseVector()));

builder.Services.AddHttpClient<VoyageEmbeddingClient>((sp, client) =>
{
    var apiKey = sp.GetRequiredService<IConfiguration>()["VOYAGE_API_KEY"];
    if (string.IsNullOrWhiteSpace(apiKey))
        throw new InvalidOperationException("VOYAGE_API_KEY is not configured.");

    client.BaseAddress = new Uri("https://api.voyageai.com/v1/");
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
});

builder.Services.AddSingleton<IDocumentProcessingQueue, DocumentProcessingQueue>();
builder.Services.AddHostedService<DocumentProcessingWorker>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapDocumentEndpoints();

app.Run();
