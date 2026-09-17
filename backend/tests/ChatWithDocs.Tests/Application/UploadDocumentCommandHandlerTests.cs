using ChatWithDocs.Application.Documents.Commands;
using ChatWithDocs.Application.Interfaces;
using ChatWithDocs.Domain.Entities;
using ChatWithDocs.Domain.Enums;
using NSubstitute;

namespace ChatWithDocs.Tests.Application;

public class UploadDocumentCommandHandlerTests
{
    private readonly IDocumentRepository _documentRepository = Substitute.For<IDocumentRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDocumentProcessingQueue _queue = Substitute.For<IDocumentProcessingQueue>();

    private UploadDocumentCommandHandler CreateHandler() =>
        new(_documentRepository, _unitOfWork, _queue);

    [Fact]
    public async Task Handle_PersistsANewDocumentWithProcessingStatus()
    {
        var handler = CreateHandler();
        var content = new byte[] { 1, 2, 3, 4 };

        await handler.Handle(new UploadDocumentCommand("report.pdf", content), CancellationToken.None);

        await _documentRepository.Received(1).AddAsync(
            Arg.Is<Document>(d =>
                d.FileName == "report.pdf" &&
                d.Status == DocumentStatus.Processing &&
                d.Id != Guid.Empty &&
                d.CreatedAt == d.UpdatedAt),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EnqueuesAProcessingJobCarryingTheSameDocumentIdAndTheRawContent()
    {
        var handler = CreateHandler();
        var content = new byte[] { 9, 8, 7 };
        Document? persistedDocument = null;
        _documentRepository
            .When(r => r.AddAsync(Arg.Any<Document>(), Arg.Any<CancellationToken>()))
            .Do(call => persistedDocument = call.Arg<Document>());

        await handler.Handle(new UploadDocumentCommand("scan.pdf", content), CancellationToken.None);

        Assert.NotNull(persistedDocument);
        _queue.Received(1).Enqueue(Arg.Is<DocumentProcessingJob>(job =>
            job.DocumentId == persistedDocument!.Id &&
            job.PdfContent == content));
    }

    [Fact]
    public async Task Handle_ReturnsTheNewDocumentIdWithProcessingStatus()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new UploadDocumentCommand("notes.pdf", [1]), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(nameof(DocumentStatus.Processing), result.Status);
    }

    [Fact]
    public async Task Handle_PersistsBeforeEnqueueing_SoTheJobNeverRacesAheadOfItsOwnDocumentRow()
    {
        var handler = CreateHandler();
        var callOrder = new List<string>();
        _documentRepository
            .When(r => r.AddAsync(Arg.Any<Document>(), Arg.Any<CancellationToken>()))
            .Do(_ => callOrder.Add("AddAsync"));
        _unitOfWork
            .When(u => u.SaveChangesAsync(Arg.Any<CancellationToken>()))
            .Do(_ => callOrder.Add("SaveChangesAsync"));
        _queue
            .When(q => q.Enqueue(Arg.Any<DocumentProcessingJob>()))
            .Do(_ => callOrder.Add("Enqueue"));

        await handler.Handle(new UploadDocumentCommand("order.pdf", [1]), CancellationToken.None);

        Assert.Equal(["AddAsync", "SaveChangesAsync", "Enqueue"], callOrder);
    }
}
