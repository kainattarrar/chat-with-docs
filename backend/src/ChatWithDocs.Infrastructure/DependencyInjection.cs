using System.Net.Http.Headers;
using Anthropic;
using ChatWithDocs.Application.Interfaces;
using ChatWithDocs.Infrastructure.Chat;
using ChatWithDocs.Infrastructure.Documents;
using ChatWithDocs.Infrastructure.Embeddings;
using ChatWithDocs.Infrastructure.Persistence;
using ChatWithDocs.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChatWithDocs.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("Default"),
                npgsqlOptions => npgsqlOptions.UseVector()));

        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IChunkRepository, ChunkRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddHttpClient<IEmbeddingService, VoyageEmbeddingClient>((sp, client) =>
        {
            var apiKey = sp.GetRequiredService<IConfiguration>()["VOYAGE_API_KEY"];
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("VOYAGE_API_KEY is not configured.");

            client.BaseAddress = new Uri("https://api.voyageai.com/v1/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        });

        services.AddSingleton(sp =>
        {
            var apiKey = sp.GetRequiredService<IConfiguration>()["ANTHROPIC_API_KEY"];
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("ANTHROPIC_API_KEY is not configured.");

            return new AnthropicClient { ApiKey = apiKey };
        });
        services.AddScoped<IChatService, AnthropicChatService>();

        services.AddSingleton<IDocumentProcessingQueue, DocumentProcessingQueue>();
        services.AddHostedService<DocumentProcessingWorker>();

        return services;
    }
}
