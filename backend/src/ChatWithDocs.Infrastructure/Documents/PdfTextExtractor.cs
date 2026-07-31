using System.Text;
using UglyToad.PdfPig;

namespace ChatWithDocs.Infrastructure.Documents;

public static class PdfTextExtractor
{
    public static string ExtractText(byte[] pdfBytes)
    {
        using var document = PdfDocument.Open(pdfBytes);
        var sb = new StringBuilder();

        foreach (var page in document.GetPages())
        {
            sb.AppendLine(page.Text);
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
