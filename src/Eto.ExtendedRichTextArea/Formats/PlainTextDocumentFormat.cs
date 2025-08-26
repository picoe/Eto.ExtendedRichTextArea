using Eto.ExtendedRichTextArea.Model;

namespace Eto.ExtendedRichTextArea.Formats
{
	internal class PlainTextDocumentFormat : DocumentFormat
	{
		public override string Name => "Plain Text";
		public override string Extension => ".txt";
		public override bool Load(Document document, Stream stream)
		{
			using (var reader = new StreamReader(stream))
			{
				document.Text = reader.ReadToEnd();
			}
			return true;
		}
		public override bool Save(Document document, Stream stream)
		{
			using (var writer = new StreamWriter(stream))
			{
				writer.Write(document.Text);
			}
			return true;
		}
	}
}
