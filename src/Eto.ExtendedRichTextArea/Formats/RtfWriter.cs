using Eto.Drawing;
using Eto.ExtendedRichTextArea.Model;

using System.Text;

namespace Eto.ExtendedRichTextArea.Formats
{
	internal class RtfWriter
	{
		readonly StreamWriter _writer;
		readonly Document _document;
		public RtfWriter(Document document, Stream stream)
		{
			_document = document;
			_writer = new StreamWriter(stream, Encoding.UTF8, 1024, true);
		}

		internal bool WriteDocument()
		{
			foreach (var block in _document)
			{
				WriteBlock(block);
			}
			_writer.Flush();
			return true;
		}

		private void WriteBlock(IBlockElement block)
		{
			WriteHeader();
			if (block is ParagraphElement paragraph)
			{
				WriteParagraph(paragraph);
			}
			else if (block is ListElement list)
			{
				WriteList(list);
			}
			else
			{
				// Handle other block types as needed
			}
			WriteFooter();
		}

		private void WriteFooter()
		{
			_writer.Write(@"}");
		}

		private void WriteHeader()
		{
			_writer.Write(@"{\rtf1\ansi\ansicpg1252\deff0\nouicompat\deflang1033");
		}

		private void WriteList(ListElement list)
		{
			_writer.Write(@"{\listtext");	
			
			foreach (var item in list)
			{
				_writer.Write(@"{\listitem");
				WriteBlock(item);
				_writer.Write(@"}");
			}
			_writer.Write(@"}");
		}

		private void WriteParagraph(ParagraphElement paragraph)
		{
			_writer.Write(@"{\pard");
			foreach (var inline in paragraph)
			{
				WriteInline(inline);
			}
			_writer.Write(@"}");
		}

		private void WriteInline(IInlineElement inline)
		{
			switch (inline)
			{
				case TextElement text:
					_writer.Write(text.Text);
					break;
				case ImageElement image:
					var data = ConvertImageToPng(image.Image);
					_writer.Write(@"{\pict\pngblip");
					_writer.Write(ToHexString(data));
					_writer.Write(@"}");
					break;
				default:
					throw new NotImplementedException();
			}
		}

		private static byte[] ConvertImageToPng(Image? image)
		{
			if (image is not Bitmap bitmap)
				return Array.Empty<byte>();

			using var stream = new MemoryStream();
			bitmap.Save(stream, ImageFormat.Png);
			return stream.ToArray();

		}
		public static string ToHexString(byte[] bytes, int lineLength = 78)
		{
			var sb = new StringBuilder(bytes.Length * 2);
			int count = 0;

			foreach (byte b in bytes)
			{
				sb.AppendFormat("{0:X2}", b);
				count += 2;

				// insert line breaks every ~78 characters (RTF readers like it broken up)
				if (count >= lineLength)
				{
					sb.AppendLine();
					count = 0;
				}
			}

			return sb.ToString();
		}		
	}
}
