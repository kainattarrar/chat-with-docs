using backend.Data;
using backend.Models;
using backend.Services;

namespace backend.Endpoints;

public static class DocumentEndpoints
{
    private const long MaxFileSizeBytes = 20 * 1024 * 1024;

    public static void MapDocumentEndpoints(this WebApplication app)
    {
        app.MapPost("/api/documents", UploadDocumentAsync)
            .DisableAntiforgery();
    }

    private static async Task<IResult> UploadDocumentAsync(
        IFormFile? file,
        AppDbContext db,
        IDocumentProcessingQueue queue,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return Results.BadRequest(new { error = "No file was uploaded." });

        if (file.Length > MaxFileSizeBytes)
            return Results.BadRequest(new { error = $"File exceeds the {MaxFileSizeBytes / (1024 * 1024)} MB limit." });

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        var pdfBytes = memoryStream.ToArray();

        if (!IsPdfSignature(pdfBytes))
            return Results.BadRequest(new { error = "Only PDF files are supported." });

        var now = DateTime.UtcNow;
        var document = new Document
        {
            Id = Guid.NewGuid(),
            FileName = file.FileName,
            Status = DocumentStatus.Processing,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Documents.Add(document);
        await db.SaveChangesAsync(cancellationToken);

        queue.Enqueue(new DocumentProcessingJob(document.Id, pdfBytes));

        return Results.Accepted(value: new { id = document.Id, status = document.Status.ToString() });
    }

    private static bool IsPdfSignature(byte[] bytes) =>
        bytes.Length >= 5 &&
        bytes[0] == (byte)'%' &&
        bytes[1] == (byte)'P' &&
        bytes[2] == (byte)'D' &&
        bytes[3] == (byte)'F' &&
        bytes[4] == (byte)'-';
}
