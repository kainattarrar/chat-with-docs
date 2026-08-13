namespace ChatWithDocs.Application.Interfaces;

public interface ITitleGenerationService
{
    Task<string> GenerateTitleAsync(string firstQuestion, CancellationToken cancellationToken);
}
