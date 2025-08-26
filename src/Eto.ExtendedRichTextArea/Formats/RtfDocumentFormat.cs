using Eto.ExtendedRichTextArea.Model;

namespace Eto.ExtendedRichTextArea.Formats
{
	internal class RtfDocumentFormat : DocumentFormat
	{
		public override string Name => "Rich Text Format";
		public override string Extension => ".rtf";

		public override bool Load(Document document, Stream stream)
		{
			using (var reader = new StreamReader(stream))
			{
				var text = reader.ReadToEnd();
				return false;
				// var rtfReader = new RtfReader();
				// var doc = rtfReader.ReadDocument(text);
				// document.Clear();
				// foreach (var block in doc)
				// {
				// 	document.Add(block);
				// }
				// return true;
			}
		}

		public override bool Save(Document document, Stream stream)
		{
			var writer = new RtfWriter(document, stream);
			if (!writer.WriteDocument())
				return false;
			return true;
		}
	}
}
