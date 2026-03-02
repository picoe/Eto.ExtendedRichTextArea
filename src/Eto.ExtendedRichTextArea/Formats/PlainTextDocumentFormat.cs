using Eto.ExtendedRichTextArea.Model;
using Eto.Forms;

namespace Eto.ExtendedRichTextArea.Formats;

internal class PlainTextDocumentFormat : DocumentFormat
{
	public override string Name => "Plain Text";
	public override string Extension => ".txt";
	
	public override bool Load(DocumentRange document, Stream stream)
	{
		var reader = new StreamReader(stream);
		document.Text = reader.ReadToEnd();
		return true;
	}

	public override bool Save(DocumentRange document, Stream stream)
	{
		var writer = new StreamWriter(stream);
		writer.Write(document.Text);
		writer.Flush();
		return true;
	}

	public override bool ReadDataObject(DocumentRange range, IDataObject dataObject)
	{
		if (dataObject.ContainsText)
		{
			var text = dataObject.Text;
			return LoadFromString(range, text);
		}
		return false;
	}

	public override void WriteDataObject(DocumentRange range, IDataObject dataObject)
	{
		var text = range.Text;
		dataObject.Text = text;
	}

	public override bool CanReadDataObject(Clipboard clipboard)
	{
		return clipboard.ContainsText;
	}
}
