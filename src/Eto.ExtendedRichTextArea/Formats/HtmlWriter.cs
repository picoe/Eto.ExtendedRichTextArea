using System.Globalization;
using System.Net;
using System.Text;
using System.Linq;

using Eto.Drawing;
using Eto.ExtendedRichTextArea.Model;

namespace Eto.ExtendedRichTextArea.Formats;

internal class HtmlWriter
{
	readonly DocumentRange _range;
	readonly StreamWriter _writer;
	readonly List<IBlockElement> _blocks;
	readonly Color? _defaultForegroundColor;
	readonly bool _hasDefaultForegroundColor;

	public HtmlWriter(DocumentRange range, Stream stream)
	{
		_range = range;
		_writer = new StreamWriter(stream, Encoding.UTF8, 1024, true);
		_blocks = IsFullDocumentRange(range)
			? range.Document.OfType<IBlockElement>().ToList()
			: _range.GetElements().OfType<IBlockElement>().ToList();
		if (_range.Document.DefaultAttributes.Foreground is SolidBrush defaultForeground)
		{
			_defaultForegroundColor = defaultForeground.Color;
			_hasDefaultForegroundColor = true;
		}
	}

	static bool IsFullDocumentRange(DocumentRange range) => range.Start == 0 && range.End == range.Document.Length;

	internal bool WriteDocument()
	{
		_writer.WriteLine("<!doctype html>");
		_writer.WriteLine("<html>");
		_writer.WriteLine("<body>");
		foreach (var block in _blocks)
		{
			WriteBlock(block);
		}
		_writer.WriteLine("</body>");
		_writer.WriteLine("</html>");
		_writer.Flush();
		return true;
	}

	void WriteBlock(IBlockElement block)
	{
		switch (block)
		{
			case ParagraphElement paragraph:
				WriteParagraph(paragraph);
				break;
			case ListElement list:
				WriteList(list);
				break;
		}
	}

	void WriteParagraph(ParagraphElement paragraph)
	{
		_writer.Write("<p>");
		foreach (var inline in paragraph)
			WriteInline(inline);
		_writer.WriteLine("</p>");
	}

	void WriteList(ListElement list)
	{
		var tag = IsOrderedList(list) ? "ol" : "ul";
		var currentLevel = 0;
		_writer.Write('<');
		_writer.Write(tag);
		_writer.WriteLine(">");

		foreach (var item in list)
		{
			// Open nested list tags when level increases
			while (item.Level > currentLevel)
			{
				_writer.Write('<');
				_writer.Write(tag);
				_writer.WriteLine(">");
				currentLevel++;
			}
			// Close nested list tags when level decreases
			while (item.Level < currentLevel)
			{
				_writer.Write("</");
				_writer.Write(tag);
				_writer.WriteLine(">");
				currentLevel--;
			}
			_writer.Write("<li>");
			foreach (var inline in item)
				WriteInline(inline);
			_writer.WriteLine("</li>");
		}

		// Close any remaining open nested tags
		while (currentLevel > 0)
		{
			_writer.Write("</");
			_writer.Write(tag);
			_writer.WriteLine(">");
			currentLevel--;
		}

		_writer.Write("</");
		_writer.Write(tag);
		_writer.WriteLine(">");
	}

	void WriteInline(IInlineElement inline)
	{
		switch (inline)
		{
			case TextElement text:
				WriteText(text);
				break;
			case ImageElement image:
				WriteImage(image);
				break;
		}
	}

	void WriteText(TextElement text)
	{
		var attributes = text.ActualAttributes;
		var rawText = text.Text ?? string.Empty;
		var preserveWhitespace = ShouldPreserveWhitespace(rawText);
		var style = BuildStyle(attributes, preserveWhitespace);
		var encoded = WebUtility.HtmlEncode(rawText);
		encoded = encoded
			.Replace("\r\n", "\n")
			.Replace("\r", "\n")
			.Replace("\t", "&#9;")
			.Replace("\n", "<br/>")
			.Replace(((char)SpecialCharacters.SoftBreakCharacter).ToString(), "<br/>");

		if (string.IsNullOrEmpty(style))
		{
			_writer.Write(encoded);
			return;
		}

		_writer.Write("<span style=\"");
		_writer.Write(style);
		_writer.Write("\">");
		_writer.Write(encoded);
		_writer.Write("</span>");
	}

	static bool ShouldPreserveWhitespace(string text)
	{
		if (string.IsNullOrEmpty(text))
			return false;
		if (text.IndexOf('\t') >= 0)
			return true;
		if (text.Length > 1 && (text[0] == ' ' || text[text.Length - 1] == ' '))
			return true;
		return text.IndexOf("  ", StringComparison.Ordinal) >= 0;
	}

	string BuildStyle(Attributes attributes, bool preserveWhitespace)
	{
		var sb = new StringBuilder();
		var font = attributes.Font;
		var style = attributes.Typeface?.FontStyle ?? font?.Typeface?.FontStyle ?? FontStyle.None;

		if (font?.Family?.Name is string familyName && !string.IsNullOrWhiteSpace(familyName))
			AppendStyle(sb, "font-family", QuoteCssIfNeeded(familyName));
		if (attributes.Size is float size && size > 0)
			AppendStyle(sb, "font-size", $"{size.ToString(CultureInfo.InvariantCulture)}pt");
		if (style.HasFlag(FontStyle.Bold))
			AppendStyle(sb, "font-weight", "bold");
		if (style.HasFlag(FontStyle.Italic))
			AppendStyle(sb, "font-style", "italic");
		if (attributes.Foreground is SolidBrush fg && !IsDefaultForegroundColor(fg.Color))
			AppendStyle(sb, "color", ToCssColor(fg.Color));
		if (attributes.Background is SolidBrush bg)
			AppendStyle(sb, "background-color", ToCssColor(bg.Color));

		var decorations = new List<string>();
		if (attributes.Underline == true)
			decorations.Add("underline");
		if (attributes.Strikethrough == true)
			decorations.Add("line-through");
		if (decorations.Count > 0)
			AppendStyle(sb, "text-decoration", string.Join(" ", decorations));

		if (attributes.Superscript == true)
			AppendStyle(sb, "vertical-align", "super");
		else if (attributes.Subscript == true)
			AppendStyle(sb, "vertical-align", "sub");
		if (preserveWhitespace)
			AppendStyle(sb, "white-space", "pre");

		return sb.ToString();
	}

	bool IsDefaultForegroundColor(Color color)
	{
		return _hasDefaultForegroundColor && color == _defaultForegroundColor;
	}

	static void AppendStyle(StringBuilder sb, string key, string value)
	{
		if (sb.Length > 0)
			sb.Append(';');
		sb.Append(key);
		sb.Append(':');
		sb.Append(value);
	}

	static string QuoteCssIfNeeded(string value)
	{
		if (value.IndexOf(' ') < 0)
			return value;
		return $"'{value.Replace("'", "\\'")}'";
	}

	static string ToCssColor(Color color)
	{
		return $"#{color.Rb:X2}{color.Gb:X2}{color.Bb:X2}";
	}

	void WriteImage(ImageElement image)
	{
		if (image.Image is not Bitmap bitmap)
			return;

		var data = bitmap.ToByteArray(ImageFormat.Png);
		var base64 = Convert.ToBase64String(data);

		_writer.Write("<img src=\"data:image/png;base64,");
		_writer.Write(base64);
		_writer.Write('"');
		if (image.Size.Width > 0)
		{
			_writer.Write(" width=\"");
			_writer.Write(((int)Math.Round(image.Size.Width)).ToString(CultureInfo.InvariantCulture));
			_writer.Write('"');
		}
		if (image.Size.Height > 0)
		{
			_writer.Write(" height=\"");
			_writer.Write(((int)Math.Round(image.Size.Height)).ToString(CultureInfo.InvariantCulture));
			_writer.Write('"');
		}
		_writer.Write(" />");
	}

	static bool IsOrderedList(ListElement list)
	{
		if (list.Type is NumericListType || list.Type is AlphabeticalListType || list.Type is RomanNumeralListType)
			return true;

		if (list.Type is MultipleListType multiple && multiple.Types.Count > 0)
		{
			var first = multiple.Types[0];
			return first is NumericListType || first is AlphabeticalListType || first is RomanNumeralListType;
		}

		return false;
	}
}
