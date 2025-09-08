using Eto.ExtendedRichTextArea.Formats;
using Eto.ExtendedRichTextArea.Model;

namespace Eto.ExtendedRichTextArea;

public abstract class DocumentFormat
{
	public static DocumentFormat Rtf => new RtfDocumentFormat();
	public static DocumentFormat PlainText => new PlainTextDocumentFormat();

	public abstract string Name { get; }
	public abstract string Extension { get; }

	public abstract bool Load(Document document, Stream stream);
	public abstract bool Save(Document document, Stream stream);
}
