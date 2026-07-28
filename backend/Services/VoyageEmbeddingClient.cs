using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace backend.Services;

public class VoyageEmbeddingClient(HttpClient httpClient)
{
    private const string Model = "voyage-4-lite";
    private const int OutputDimension = 1024;
    private const int MaxBatchSize = 100;

    public async Task<List<float[]>> EmbedDocumentsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken)
    {
        var embeddings = new List<float[]>(texts.Count);

        foreach (var batch in texts.Chunk(MaxBatchSize))
        {
            var request = new VoyageEmbeddingRequest
            {
                Input = batch,
                Model = Model,
                InputType = "document",
                OutputDimension = OutputDimension,
            };

            using var response = await httpClient.PostAsJsonAsync("embeddings", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<VoyageEmbeddingResponse>(cancellationToken)
                ?? throw new InvalidOperationException("Voyage API returned an empty response.");

            embeddings.AddRange(result.Data.OrderBy(d => d.Index).Select(d => d.Embedding));
        }

        return embeddings;
    }

    private class VoyageEmbeddingRequest
    {
        [JsonPropertyName("input")]
        public required IReadOnlyList<string> Input { get; init; }

        [JsonPropertyName("model")]
        public required string Model { get; init; }

        [JsonPropertyName("input_type")]
        public required string InputType { get; init; }

        [JsonPropertyName("output_dimension")]
        public required int OutputDimension { get; init; }
    }

    private class VoyageEmbeddingResponse
    {
        [JsonPropertyName("data")]
        public required List<VoyageEmbeddingData> Data { get; init; }
    }

    private class VoyageEmbeddingData
    {
        [JsonPropertyName("embedding")]
        public required float[] Embedding { get; init; }

        [JsonPropertyName("index")]
        public required int Index { get; init; }
    }
}
