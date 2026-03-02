using Eto.ExtendedRichTextArea.Model;
using Eto.Forms;

using System.Linq;

namespace Eto.ExtendedRichTextArea.Formats;

internal class RtfDocumentFormat : DocumentFormat
{
	public override string Name => "Rich Text Format";
	public override string Extension => ".rtf";

	public override bool Load(DocumentRange range, Stream stream)
	{
		var reader = new StreamReader(stream);
		var text = reader.ReadToEnd();
		if (string.IsNullOrWhiteSpace(text))
			return false;

		try
		{
			var rtfReader = new RtfReader();
			var parsed = rtfReader.ReadDocument(text);
			var blocks = parsed.Select(block => (IBlockElement)block.Clone()).ToList();
			range.ReplaceWithBlocks(blocks);
			return true;
		}
		catch
		{
			return false;
		}
	}

	public override bool Save(DocumentRange range, Stream stream)
	{
		var writer = new RtfWriter(range, stream);
		if (!writer.WriteDocument())
			return false;
		return true;
	}
	
	static readonly string[] s_types =
		Platform.Instance.IsMac 
			? ["public.rtf"]
		: Platform.Instance.IsGtk 
			? ["application/rtf", "text/rtf"]
		: ["Rich Text Format"];

	public override bool ReadDataObject(DocumentRange range, IDataObject dataObject)
	{
		foreach (var type in s_types)
		{
			if (!dataObject.Contains(type))
				continue;

			var rtf = dataObject.GetString(type);
			if (string.IsNullOrWhiteSpace(rtf))
				continue;

			return LoadFromString(range, rtf);
		}
		return false;
	}
	
	public override void WriteDataObject(DocumentRange range, IDataObject dataObject)
	{
		var rtf = SaveToString(range);
		if (!string.IsNullOrWhiteSpace(rtf))
		{
			foreach (var type in s_types)
				dataObject.SetString(rtf, type);
		}
	}

	public override bool CanReadDataObject(Clipboard clipboard)
	{
		foreach (var type in s_types)
		{
			if (clipboard.Contains(type))
				return true;
		}
		return false;
	}
}
