using Eto.ExtendedRichTextArea.Model;
using Eto.Forms;

using System.Diagnostics;
using System.Linq;

namespace Eto.ExtendedRichTextArea.Formats;

internal class HtmlDocumentFormat : DocumentFormat
{
	public override string Name => "HTML";
	public override string Extension => ".html";

	public override bool Load(DocumentRange range, Stream stream)
	{
		var reader = new StreamReader(stream);
		var html = reader.ReadToEnd();
		if (string.IsNullOrWhiteSpace(html))
			return false;

		try
		{
			var htmlReader = new HtmlReader();
			var parsed = htmlReader.ReadDocument(html);
			var blocks = parsed.Select(block => (IBlockElement)block.Clone()).ToList();
			range.ReplaceWithBlocks(blocks);
			return true;
		}
		catch (Exception ex)
		{
			Trace.WriteLine($"Error loading HTML: {ex}");
			return false;
		}
	}

	public override bool Save(DocumentRange range, Stream stream)
	{
		var writer = new HtmlWriter(range, stream);
		return writer.WriteDocument();
	}

	public override bool ReadDataObject(DocumentRange range, IDataObject dataObject)
	{
		if (dataObject.ContainsHtml)
		{
			var html = dataObject.Html;
			return LoadFromString(range, html);
		}
		return false;
	}
	
	override public bool CanReadDataObject(Clipboard clipboard)
	{
		return clipboard.ContainsHtml;
	}

	public override void WriteDataObject(DocumentRange range, IDataObject dataObject)
	{
		var html = SaveToString(range);
		dataObject.Html = html;
	}
}
