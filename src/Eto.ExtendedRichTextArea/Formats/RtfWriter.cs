using Eto.Drawing;
using Eto.ExtendedRichTextArea.Model;

using System.Text;
using System.Globalization;
using System.Linq;

namespace Eto.ExtendedRichTextArea.Formats;

internal class RtfWriter
{
	readonly StreamWriter _writer;
	readonly DocumentRange _range;
	readonly List<IBlockElement> _blocks;
	readonly Dictionary<string, int> _fontIndexes = new(StringComparer.OrdinalIgnoreCase);
	readonly Dictionary<Color, int> _colorIndexes = new();
	readonly Color? _defaultForegroundColor;
	readonly bool _hasDefaultForegroundColor;

	public RtfWriter(DocumentRange document, Stream stream)
	{
		_range = document;
		_writer = new StreamWriter(stream, Encoding.UTF8, 1024, true);
		_blocks = IsFullDocumentRange(document)
			? document.Document.OfType<IBlockElement>().ToList()
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
		CollectResources();
		WriteHeader();
		foreach (var element in _blocks)
		{
			WriteBlock(element);
		}
		WriteFooter();
		_writer.Flush();
		return true;
	}

	private void WriteBlock(IElement block)
	{
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
	}

	private void WriteFooter()
	{
		_writer.Write(@"}");
	}

	private void WriteHeader()
	{
		_writer.Write(@"{\rtf1\ansi\ansicpg1252\deff0\nouicompat\deflang1033");
		WriteFontTable();
		WriteColorTable();
		_writer.WriteLine();
	}

	private void WriteList(ListElement list)
	{
		_writer.Write(@"{\listtext");

		foreach (var item in list)
		{
			_writer.Write(@"{\listitem");
			if (item.Level > 0)
			{
				// RTF indent is in twips (1/20 point). 360 twips ~= 0.25 in.
				_writer.Write(@"\li");
				_writer.Write((item.Level * 360).ToString(CultureInfo.InvariantCulture));
			}
			WriteParagraph(item);
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
				WriteText(text);
				break;
			case ImageElement image:
				var data = ConvertImageToPng(image.Image);
				_writer.Write(@"{\pict\pngblip");
				if (image.Size.Width > 0 && image.Size.Height > 0)
				{
					var widthTwips = (int)Math.Round(image.Size.Width * 15f);
					var heightTwips = (int)Math.Round(image.Size.Height * 15f);
					_writer.Write(@"\picw");
					_writer.Write(widthTwips.ToString(CultureInfo.InvariantCulture));
					_writer.Write(@"\pich");
					_writer.Write(heightTwips.ToString(CultureInfo.InvariantCulture));
					_writer.Write(@"\picwgoal");
					_writer.Write(widthTwips.ToString(CultureInfo.InvariantCulture));
					_writer.Write(@"\pichgoal");
					_writer.Write(heightTwips.ToString(CultureInfo.InvariantCulture));
				}
				_writer.WriteLine();
				_writer.Write(ToHexString(data));
				_writer.Write(@"}");
				break;
			default:
				throw new NotImplementedException();
		}
	}

	private void WriteText(TextElement text)
	{
		var attributes = text.ActualAttributes;
		var fontIndex = GetFontIndex(attributes);
		var foregroundIndex = GetForegroundColorIndex(attributes.Foreground);
		var backgroundIndex = GetColorIndex(attributes.Background);

		_writer.Write(@"{\plain");
		_writer.Write(@"\f");
		_writer.Write(fontIndex.ToString(CultureInfo.InvariantCulture));

		var size = attributes.Size ?? attributes.Font?.Size ?? Document.GetDefaultFont().Size;
		_writer.Write(@"\fs");
		_writer.Write(((int)Math.Round(size * 2f)).ToString(CultureInfo.InvariantCulture));

		var style = attributes.Font?.Typeface?.FontStyle ?? attributes.Typeface?.FontStyle ?? FontStyle.None;
		if (attributes.Bold.HasValue)
		{
			style = attributes.Bold.Value ? style | FontStyle.Bold : style & ~FontStyle.Bold;
		}
		if (attributes.Italic.HasValue)
		{
			style = attributes.Italic.Value ? style | FontStyle.Italic : style & ~FontStyle.Italic;
		}
		_writer.Write(style.HasFlag(FontStyle.Bold) ? @"\b" : @"\b0");
		_writer.Write(style.HasFlag(FontStyle.Italic) ? @"\i" : @"\i0");
		_writer.Write(attributes.Underline == true ? @"\ul" : @"\ulnone");
		_writer.Write(attributes.Strikethrough == true ? @"\strike" : @"\strike0");

		if (attributes.Superscript == true)
			_writer.Write(@"\super");
		else if (attributes.Subscript == true)
			_writer.Write(@"\sub");
		else
			_writer.Write(@"\nosupersub");

		if (foregroundIndex > 0)
		{
			_writer.Write(@"\cf");
			_writer.Write(foregroundIndex.ToString(CultureInfo.InvariantCulture));
		}
		if (backgroundIndex > 0)
		{
			_writer.Write(@"\highlight");
			_writer.Write(backgroundIndex.ToString(CultureInfo.InvariantCulture));
		}
		_writer.Write(" ");
		WriteEscapedText(text.Text);
		_writer.Write("}");
	}

	private void WriteEscapedText(string text)
	{
		for (var i = 0; i < text.Length; i++)
		{
			var ch = text[i];
			if (char.IsHighSurrogate(ch) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
			{
				var codePoint = char.ConvertToUtf32(ch, text[i + 1]);
				WriteUnicodeCodePoint(codePoint);
				i++;
				continue;
			}

			if (ch == '\\' || ch == '{' || ch == '}')
			{
				_writer.Write('\\');
				_writer.Write(ch);
				continue;
			}

			if (ch == SpecialCharacters.TabCharacter)
			{
				_writer.Write(@"\tab ");
				continue;
			}

			if (ch == SpecialCharacters.SoftBreakCharacter || ch == SpecialCharacters.HardBreakCharacter)
			{
				_writer.Write(@"\line ");
				continue;
			}

			if (ch >= 0x20 && ch <= 0x7E)
			{
				_writer.Write(ch);
				continue;
			}

			WriteUnicodeCodePoint(ch);
		}
	}

	private void WriteUnicodeCodePoint(int codePoint)
	{
		if (codePoint <= short.MaxValue)
		{
			_writer.Write(@"\u");
			_writer.Write(codePoint.ToString(CultureInfo.InvariantCulture));
			_writer.Write('?');
			return;
		}

		// RTF \u uses 16-bit signed values. Encode non-BMP points as surrogate \u sequences.
		var utf16 = char.ConvertFromUtf32(codePoint);
		foreach (var ch in utf16)
		{
			_writer.Write(@"\u");
			_writer.Write(((short)ch).ToString(CultureInfo.InvariantCulture));
			_writer.Write('?');
		}
	}

	private void CollectResources()
	{
		_fontIndexes.Clear();
		_colorIndexes.Clear();

		_fontIndexes[Document.GetDefaultFont().Family.Name] = 0;

		foreach (var block in _blocks)
		{
			if (block is ParagraphElement paragraph)
				CollectParagraphResources(paragraph);
			else if (block is ListElement list)
			{
				foreach (var item in list)
					CollectParagraphResources(item);
			}
		}
	}

	private void CollectParagraphResources(ParagraphElement paragraph)
	{
		foreach (var inline in paragraph)
		{
			if (inline is not TextElement text)
				continue;

			var attributes = text.ActualAttributes;
			var familyName = attributes.Font?.Family?.Name
				?? attributes.Family?.Name
				?? Document.GetDefaultFont().Family.Name;

			if (!_fontIndexes.ContainsKey(familyName))
				_fontIndexes[familyName] = _fontIndexes.Count;

			AddForegroundColor(attributes.Foreground);
			AddColor(attributes.Background);
		}
	}

	private void WriteFontTable()
	{
		if (_fontIndexes.Count == 0)
			_fontIndexes[Document.GetDefaultFont().Family.Name] = 0;

		_writer.Write(@"{\fonttbl");
		foreach (var entry in _fontIndexes.OrderBy(r => r.Value))
		{
			_writer.Write(@"{\f");
			_writer.Write(entry.Value.ToString(CultureInfo.InvariantCulture));
			_writer.Write(@"\fnil ");
			_writer.Write(EscapeControlText(entry.Key));
			_writer.Write(";}");
		}
		_writer.Write("}");
	}

	private void WriteColorTable()
	{
		_writer.Write(@"{\colortbl ;");
		foreach (var entry in _colorIndexes.OrderBy(r => r.Value))
		{
			var color = entry.Key;
			_writer.Write(@"\red");
			_writer.Write(color.Rb.ToString(CultureInfo.InvariantCulture));
			_writer.Write(@"\green");
			_writer.Write(color.Gb.ToString(CultureInfo.InvariantCulture));
			_writer.Write(@"\blue");
			_writer.Write(color.Bb.ToString(CultureInfo.InvariantCulture));
			_writer.Write(';');
		}
		_writer.Write("}");
	}

	private int GetFontIndex(Attributes attributes)
	{
		var family = attributes.Font?.Family?.Name
			?? attributes.Family?.Name
			?? Document.GetDefaultFont().Family.Name;

		if (_fontIndexes.TryGetValue(family, out var index))
			return index;

		index = _fontIndexes.Count;
		_fontIndexes[family] = index;
		return index;
	}

	private int GetColorIndex(Brush? brush)
	{
		if (brush is not SolidBrush solid)
			return 0;

		if (_colorIndexes.TryGetValue(solid.Color, out var index))
			return index;

		index = _colorIndexes.Count + 1; // index 0 is auto/default color in RTF.
		_colorIndexes[solid.Color] = index;
		return index;
	}

	private int GetForegroundColorIndex(Brush? brush)
	{
		if (brush is not SolidBrush solid)
			return 0;
		if (IsDefaultForegroundColor(solid.Color))
			return 0;

		return GetColorIndex(brush);
	}

	private void AddColor(Brush? brush)
	{
		if (brush is not SolidBrush solid)
			return;
		if (_colorIndexes.ContainsKey(solid.Color))
			return;
		_colorIndexes[solid.Color] = _colorIndexes.Count + 1;
	}

	private void AddForegroundColor(Brush? brush)
	{
		if (brush is not SolidBrush solid)
			return;
		if (IsDefaultForegroundColor(solid.Color))
			return;

		AddColor(brush);
	}

	private bool IsDefaultForegroundColor(Color color)
	{
		return _hasDefaultForegroundColor && color == _defaultForegroundColor;
	}

	private static string EscapeControlText(string value)
	{
		if (string.IsNullOrEmpty(value))
			return string.Empty;

		return value
			.Replace(@"\", @"\\")
			.Replace("{", @"\{")
			.Replace("}", @"\}");
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
