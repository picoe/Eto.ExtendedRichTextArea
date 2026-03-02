using Eto.ExtendedRichTextArea.Formats;
using Eto.ExtendedRichTextArea.Model;
using Eto.Forms;

namespace Eto.ExtendedRichTextArea;

public abstract class DocumentFormat
{
	public static DocumentFormat Rtf { get; } = new RtfDocumentFormat();
	public static DocumentFormat Html { get; } = new HtmlDocumentFormat();
	public static DocumentFormat PlainText { get; } = new PlainTextDocumentFormat();

	public static IList<DocumentFormat> AllFormats { get; } = new List<DocumentFormat> {
		Html,
		Rtf,
		PlainText 
	};

	public abstract string Name { get; }
	public abstract string Extension { get; }

	public abstract bool Load(DocumentRange range, Stream stream);
	public abstract bool Save(DocumentRange range, Stream stream);

	public string SaveToString(DocumentRange range)
	{
		using var ms = new MemoryStream();
		if (!Save(range, ms))
			return string.Empty;
		ms.Seek(0, SeekOrigin.Begin);

		using var reader = new StreamReader(ms);
		return reader.ReadToEnd();
	}

	public bool LoadFromString(DocumentRange range, string data)
	{
		using var ms = new MemoryStream();
		using var writer = new StreamWriter(ms);
		writer.Write(data);
		writer.Flush();
		ms.Seek(0, SeekOrigin.Begin);
		return Load(range, ms);
	}

	public abstract bool ReadDataObject(DocumentRange range, IDataObject dataObject);
	public abstract void WriteDataObject(DocumentRange range, IDataObject dataObject);
	public abstract bool CanReadDataObject(Clipboard clipboard);
}
