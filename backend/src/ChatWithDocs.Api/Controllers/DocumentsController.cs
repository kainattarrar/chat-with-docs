using ChatWithDocs.Application.Common;
using ChatWithDocs.Application.Documents.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ChatWithDocs.Api.Controllers;

[ApiController]
[Route("api/documents")]
public class DocumentsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file was uploaded." });

        if (file.Length > Constants.MaxDocumentSizeBytes)
            return BadRequest(new { error = $"File exceeds the {Constants.MaxDocumentSizeBytes / (1024 * 1024)} MB limit." });

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        var pdfBytes = memoryStream.ToArray();

        if (!IsPdfSignature(pdfBytes))
            return BadRequest(new { error = "Only PDF files are supported." });

        var result = await mediator.Send(new UploadDocumentCommand(file.FileName, pdfBytes), cancellationToken);

        return Accepted(value: new { id = result.Id, status = result.Status });
    }

    private static bool IsPdfSignature(byte[] bytes) =>
        bytes.Length >= 5 &&
        bytes[0] == (byte)'%' &&
        bytes[1] == (byte)'P' &&
        bytes[2] == (byte)'D' &&
        bytes[3] == (byte)'F' &&
        bytes[4] == (byte)'-';
}
