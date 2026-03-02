using Eto.Drawing;
using Eto.ExtendedRichTextArea.Model;

using System.Globalization;
using System.Linq;
using System.Text;

namespace Eto.ExtendedRichTextArea.Formats;

internal class RtfReader
{
	enum GroupKind
	{
		Unknown,
		Document,
		Paragraph,
		List,
		ListItem,
		Picture,
		FontTable,
		FontEntry,
		ColorTable
	}

	sealed class StyleState
	{
		public bool Bold { get; set; }
		public bool Italic { get; set; }
		public bool Underline { get; set; }
		public bool Strikethrough { get; set; }
		public bool Superscript { get; set; }
		public bool Subscript { get; set; }
		public float? FontSize { get; set; }
		public int? FontIndex { get; set; }
		public int? ForegroundColorIndex { get; set; }
		public int? BackgroundColorIndex { get; set; }

		public StyleState Clone()
		{
			return new StyleState
			{
				Bold = Bold,
				Italic = Italic,
				Underline = Underline,
				Strikethrough = Strikethrough,
				Superscript = Superscript,
				Subscript = Subscript,
				FontSize = FontSize,
				FontIndex = FontIndex,
				ForegroundColorIndex = ForegroundColorIndex,
				BackgroundColorIndex = BackgroundColorIndex
			};
		}
	}

	sealed class GroupContext
	{
		public GroupContext(GroupContext? parent)
		{
			Parent = parent;
			Style = parent?.Style.Clone() ?? new StyleState();
		}

		public GroupContext? Parent { get; }
		public GroupKind Kind { get; set; } = GroupKind.Unknown;
		public bool IgnoreDestination { get; set; }
		public StyleState Style { get; }
		public StringBuilder Text { get; } = new();
		public List<IInlineElement> Inlines { get; } = new();
		public List<IBlockElement> Blocks { get; } = new();

		public int? FontEntryIndex { get; set; }
		public string? FontEntryFamilyHint { get; set; }
		public StringBuilder FlatFontEntryText { get; } = new();
		public int ColorRed { get; set; }
		public int ColorGreen { get; set; }
		public int ColorBlue { get; set; }
		public bool HasColorComponent { get; set; }
	}

	sealed class FontTableEntry
	{
		public string RawName { get; set; } = string.Empty;
		public string? FamilyHint { get; set; }
	}

	readonly Dictionary<int, FontTableEntry> _fontTable = new();
	readonly Dictionary<int, Color> _colorTable = new();
	readonly Dictionary<string, FontTypeface> _postscriptTypefaceLookup = new(StringComparer.OrdinalIgnoreCase);
	bool _hasBuiltPostScriptLookup;
	int _escapedNewlineControlStreak;
	int _nextColorTableIndex;

	public Document ReadDocument(string rtf)
	{
		var doc = new Document();
		ParseRtf(rtf, doc);
		return doc;
	}

	private void ParseRtf(string rtf, Document document)
	{
		if (string.IsNullOrEmpty(rtf))
			return;

		_fontTable.Clear();
		_colorTable.Clear();
		_postscriptTypefaceLookup.Clear();
		_hasBuiltPostScriptLookup = false;
		_escapedNewlineControlStreak = 0;
		_nextColorTableIndex = 0;

		var stack = new Stack<GroupContext>();
		stack.Push(new GroupContext(null) { Kind = GroupKind.Document });

		var i = 0;
		while (i < rtf.Length)
		{
			var current = stack.Peek();
			var c = rtf[i];
			if (c == '{')
			{
				_escapedNewlineControlStreak = 0;
				stack.Push(new GroupContext(current));
				i++;
				continue;
			}

			if (c == '}')
			{
				_escapedNewlineControlStreak = 0;
				if (stack.Count == 1)
				{
					i++;
					continue;
				}

				var child = stack.Pop();
				FinalizeText(child);
				AttachChild(stack.Peek(), child);
				i++;
				continue;
			}

			if (c == '\\')
			{
				i = ParseControlWord(rtf, i, stack);
				continue;
			}

			// Raw CR/LF characters are RTF formatting whitespace, not document text.
			if (c == '\r' || c == '\n')
			{
				_escapedNewlineControlStreak = 0;
				i++;
				continue;
			}

			if (current.Kind == GroupKind.ColorTable && c == ';')
			{
				_escapedNewlineControlStreak = 0;
				CompleteColorEntry(current);
				i++;
				continue;
			}

			if (current.Kind == GroupKind.FontTable && current.FontEntryIndex.HasValue)
			{
				if (c == ';')
				{
					_escapedNewlineControlStreak = 0;
					CompleteFlatFontEntry(current);
					i++;
					continue;
				}

				if (!current.IgnoreDestination)
				{
					_escapedNewlineControlStreak = 0;
					current.FlatFontEntryText.Append(c);
				}
				i++;
				continue;
			}

			if (!current.IgnoreDestination)
			{
				_escapedNewlineControlStreak = 0;
				current.Text.Append(c);
			}
			i++;
		}

		while (stack.Count > 1)
		{
			var child = stack.Pop();
			FinalizeText(child);
			AttachChild(stack.Peek(), child);
		}

		var root = stack.Pop();
		foreach (var block in root.Blocks)
			document.Add(block);
	}

	private void FinalizeText(GroupContext context)
	{
		if (context.IgnoreDestination || context.Text.Length == 0)
			return;
		if (context.Kind == GroupKind.FontTable
			|| context.Kind == GroupKind.FontEntry
			|| context.Kind == GroupKind.ColorTable
			|| context.Kind == GroupKind.Picture)
			return;

		var text = context.Text.ToString();
		if (string.IsNullOrEmpty(text))
			return;

		context.Text.Clear();
		var element = new TextElement { Text = text };
		element.Attributes = CreateAttributes(context.Style);
		context.Inlines.Add(element);
	}

	private void AttachChild(GroupContext parent, GroupContext child)
	{
		if (child.IgnoreDestination)
			return;

		switch (child.Kind)
		{
			case GroupKind.FontEntry:
				AttachFontEntry(parent, child);
				return;
			case GroupKind.FontTable:
				CompleteFlatFontEntry(child);
				return;
			case GroupKind.ColorTable:
				return;
			case GroupKind.Picture:
				if (TryCreateImage(child.Text.ToString(), out var image))
				{
					if (parent.Kind == GroupKind.Paragraph || parent.Kind == GroupKind.ListItem)
						parent.Inlines.Add(image);
				}
				return;
			case GroupKind.Paragraph:
				var paragraphBlocks = child.Blocks.OfType<ParagraphElement>().ToList();
				if (child.Inlines.Count > 0 || paragraphBlocks.Count == 0)
				{
					var trailingParagraph = new ParagraphElement();
					trailingParagraph.AddRange(child.Inlines);
					paragraphBlocks.Add(trailingParagraph);
				}

				foreach (var paragraph in paragraphBlocks)
				{
					if (parent.Kind == GroupKind.ListItem || parent.Kind == GroupKind.Paragraph)
					{
						parent.Inlines.AddRange(paragraph);
					}
					else if (parent.Kind == GroupKind.List)
					{
						var item = new ListItemElement();
						item.AddRange(paragraph);
						parent.Blocks.Add(item);
					}
					else
					{
						parent.Blocks.Add(paragraph);
					}
				}
				return;
			case GroupKind.ListItem:
				var listItem = new ListItemElement();
				listItem.AddRange(child.Inlines);
				parent.Blocks.Add(listItem);
				return;
			case GroupKind.List:
				var list = new ListElement();
				foreach (var block in child.Blocks)
				{
					if (block is ListItemElement item)
					{
						list.Add(item);
						continue;
					}
					if (block is ParagraphElement paragraphItem)
					{
						var listItemFromParagraph = new ListItemElement();
						listItemFromParagraph.AddRange(paragraphItem);
						list.Add(listItemFromParagraph);
					}
				}
				if (list.Count > 0)
					parent.Blocks.Add(list);
				return;
			default:
				if (child.Blocks.Count > 0)
					parent.Blocks.AddRange(child.Blocks);
				else if (child.Inlines.Count > 0 && (parent.Kind == GroupKind.Paragraph || parent.Kind == GroupKind.ListItem))
					parent.Inlines.AddRange(child.Inlines);
				return;
		}
	}

	private void AttachFontEntry(GroupContext parent, GroupContext child)
	{
		if (parent.Kind != GroupKind.FontTable || !child.FontEntryIndex.HasValue)
			return;

		var name = child.Text
			.ToString()
			.Replace(";", string.Empty)
			.Trim();
		if (!string.IsNullOrEmpty(name))
		{
			_fontTable[child.FontEntryIndex.Value] = new FontTableEntry
			{
				RawName = name,
				FamilyHint = child.FontEntryFamilyHint
			};
		}
	}

	private void CompleteFlatFontEntry(GroupContext context)
	{
		if (context.Kind != GroupKind.FontTable || !context.FontEntryIndex.HasValue)
			return;

		var name = context.FlatFontEntryText
			.ToString()
			.Replace(";", string.Empty)
			.Trim();
		if (!string.IsNullOrEmpty(name))
		{
			_fontTable[context.FontEntryIndex.Value] = new FontTableEntry
			{
				RawName = name,
				FamilyHint = context.FontEntryFamilyHint
			};
		}

		context.FontEntryIndex = null;
		context.FontEntryFamilyHint = null;
		context.FlatFontEntryText.Clear();
	}

	private void StartFlatFontEntry(GroupContext context, int index)
	{
		CompleteFlatFontEntry(context);
		context.FontEntryIndex = index;
		context.FontEntryFamilyHint = null;
		context.FlatFontEntryText.Clear();
	}

	private void CommitParagraphBreak(GroupContext context)
	{
		if (context.IgnoreDestination || context.Kind != GroupKind.Paragraph)
			return;

		FinalizeText(context);
		var paragraph = new ParagraphElement();
		paragraph.AddRange(context.Inlines);
		context.Inlines.Clear();
		context.Blocks.Add(paragraph);
	}

	private Attributes? CreateAttributes(StyleState style)
	{
		var attributes = new Attributes();
		var hasAttributes = false;

		if (style.FontSize.HasValue)
		{
			attributes.Size = style.FontSize.Value;
			hasAttributes = true;
		}
		if (style.Underline)
		{
			attributes.Underline = true;
			hasAttributes = true;
		}
		if (style.Strikethrough)
		{
			attributes.Strikethrough = true;
			hasAttributes = true;
		}
		if (style.Superscript)
		{
			attributes.Superscript = true;
			hasAttributes = true;
		}
		else if (style.Subscript)
		{
			attributes.Subscript = true;
			hasAttributes = true;
		}
		if (style.ForegroundColorIndex.HasValue && style.ForegroundColorIndex.Value > 0 && _colorTable.TryGetValue(style.ForegroundColorIndex.Value, out var foreground))
		{
			attributes.Foreground = new SolidBrush(foreground);
			hasAttributes = true;
		}
		if (style.BackgroundColorIndex.HasValue && style.BackgroundColorIndex.Value > 0 && _colorTable.TryGetValue(style.BackgroundColorIndex.Value, out var background))
		{
			attributes.Background = new SolidBrush(background);
			hasAttributes = true;
		}

		var fontStyle = FontStyle.None;
		if (style.Bold)
			fontStyle |= FontStyle.Bold;
		if (style.Italic)
			fontStyle |= FontStyle.Italic;
		if (fontStyle != FontStyle.None || style.FontIndex.HasValue)
		{
			var fallbackSize = style.FontSize ?? Document.GetDefaultFont().Size;
			FontFamily? family = null;
			FontTypeface? typeface = null;
			if (style.FontIndex.HasValue && _fontTable.TryGetValue(style.FontIndex.Value, out var fontEntry))
			{
				(family, typeface) = ResolveFontFromRtfName(fontEntry.RawName, fontStyle, fallbackSize);
			}
			if (family == null)
			{
				family = ResolveFamilyByName(Document.GetDefaultFont().Family.Name, fallbackSize);
				typeface ??= family != null ? ResolveTypeface(family, fontStyle) : null;
			}
			else if (fontStyle != FontStyle.None && (typeface == null || !typeface.FontStyle.HasFlag(fontStyle)))
			{
				// First try to satisfy style within the resolved family (e.g. postscript entry + \b/\i).
				var resolvedTypeface = ResolveTypeface(family, fontStyle);
				if (resolvedTypeface != null && resolvedTypeface.FontStyle.HasFlag(fontStyle))
				{
					typeface = resolvedTypeface;
				}
				else
				{
					// Some platform fonts in Word RTF payloads (e.g. Aptos variants) may not expose styled faces.
					// Fall back to a default family that can satisfy the requested style (strict or best partial).
					var fallbackFamily = ResolveFamilyByName(Document.GetDefaultFont().Family.Name, fallbackSize) ?? Document.GetDefaultFont().Family;
					var fallbackTypeface = ResolveTypeface(fallbackFamily, fontStyle);
					if (fallbackTypeface != null)
					{
						family = fallbackFamily;
						typeface = fallbackTypeface;
					}
					else
					{
						// If no exact face exists, prefer a partial match (bold before italic) so effective Font style is preserved as much as possible.
						var fallbackPartialTypeface = ResolveBestPartialTypeface(fallbackFamily, fontStyle);
						if (fallbackPartialTypeface != null)
						{
							family = fallbackFamily;
							typeface = fallbackPartialTypeface;
						}
						else
						{
							typeface = ResolveBestPartialTypeface(family, fontStyle);
						}
					}
				}
			}
			if (family != null)
			{
				attributes.Family = family;
				hasAttributes = true;
			}
			if (typeface != null)
			{
				if (fontStyle == FontStyle.None)
				{
					// Preserve explicit face only when no separate style flags are active.
					attributes.Typeface = typeface;
				}
				else if (attributes.Family == null)
				{
					// For styled runs, keep family-based resolution so Bold/Italic setters don't clear the resolved face.
					attributes.Family = typeface.Family;
				}
				hasAttributes = true;
			}
		}
		if (style.Bold)
		{
			attributes.Bold = true;
			hasAttributes = true;
		}
		if (style.Italic)
		{
			attributes.Italic = true;
			hasAttributes = true;
		}

		return hasAttributes ? attributes : null;
	}

	private (FontFamily? family, FontTypeface? typeface) ResolveFontFromRtfName(string rtfName, FontStyle fontStyle, float fallbackSize)
	{
		if (string.IsNullOrWhiteSpace(rtfName))
			return (null, null);

		if (TryResolveTypefaceByPostScriptName(rtfName, out var postscriptTypeface))
		{
			var postscriptFamily = postscriptTypeface.Family;
			if (fontStyle != FontStyle.None)
			{
				var styledTypeface = ResolveTypeface(postscriptFamily, fontStyle);
				if (styledTypeface != null && styledTypeface.FontStyle.HasFlag(fontStyle))
					return (postscriptFamily, styledTypeface);

				var partialTypeface = ResolveBestPartialTypeface(postscriptFamily, fontStyle);
				if (partialTypeface != null)
					return (postscriptFamily, partialTypeface);
			}
			return (postscriptFamily, postscriptTypeface);
		}

		var family = ResolveFamilyByName(rtfName, fallbackSize);
		if (family == null)
			return (null, null);

		return (family, ResolveTypeface(family, fontStyle));
	}

	private static FontFamily? ResolveFamilyByName(string familyName, float fallbackSize)
	{
		try
		{
			return new Font(familyName, fallbackSize).Family;
		}
		catch
		{
			// Ignore unknown fonts and keep default rendering.
			return null;
		}
	}

	private bool TryResolveTypefaceByPostScriptName(string rtfName, out FontTypeface typeface)
	{
		if (!_hasBuiltPostScriptLookup)
		{
			foreach (var family in Fonts.AvailableFontFamilies)
			{
				foreach (var familyTypeface in family.Typefaces)
				{
					if (string.IsNullOrWhiteSpace(familyTypeface.PostScriptName))
						continue;
					if (!_postscriptTypefaceLookup.ContainsKey(familyTypeface.PostScriptName))
						_postscriptTypefaceLookup[familyTypeface.PostScriptName] = familyTypeface;
				}
			}
			_hasBuiltPostScriptLookup = true;
		}

		return _postscriptTypefaceLookup.TryGetValue(rtfName, out typeface!);
	}

	private static FontTypeface? ResolveTypeface(FontFamily family, FontStyle fontStyle)
	{
		if (fontStyle != FontStyle.None)
		{
			return family.Typefaces.FirstOrDefault(tf => tf.FontStyle == fontStyle)
				?? family.Typefaces.FirstOrDefault(tf => tf.FontStyle.HasFlag(fontStyle));
		}

		return family.Typefaces.FirstOrDefault(tf => !tf.Bold && !tf.Italic)
			?? family.Typefaces.FirstOrDefault(tf => tf.FontStyle == FontStyle.None)
			?? family.Typefaces.FirstOrDefault();
	}

	private static FontTypeface? ResolveBestPartialTypeface(FontFamily family, FontStyle fontStyle)
	{
		if (fontStyle == FontStyle.None)
			return ResolveTypeface(family, FontStyle.None);

		// Preserve requested emphasis as much as possible when an exact face is unavailable.
		if (fontStyle.HasFlag(FontStyle.Bold) && fontStyle.HasFlag(FontStyle.Italic))
		{
			return family.Typefaces.FirstOrDefault(tf => tf.Bold && tf.Italic)
				?? family.Typefaces.FirstOrDefault(tf => tf.Bold)
				?? family.Typefaces.FirstOrDefault(tf => tf.Italic);
		}

		if (fontStyle.HasFlag(FontStyle.Bold))
			return family.Typefaces.FirstOrDefault(tf => tf.Bold);

		if (fontStyle.HasFlag(FontStyle.Italic))
			return family.Typefaces.FirstOrDefault(tf => tf.Italic);

		return ResolveTypeface(family, FontStyle.None);
	}

	private static bool TryCreateImage(string text, out ImageElement image)
	{
		image = null!;
		if (string.IsNullOrEmpty(text))
			return false;

		var hex = new StringBuilder(text.Length);
		foreach (var ch in text)
		{
			if (IsHexDigit(ch))
				hex.Append(ch);
		}

		if (hex.Length < 2 || hex.Length % 2 != 0)
			return false;

		try
		{
			var bytes = new byte[hex.Length / 2];
			for (var i = 0; i < bytes.Length; i++)
			{
				bytes[i] = Convert.ToByte(hex.ToString(i * 2, 2), 16);
			}
			using var ms = new MemoryStream(bytes);
			var bitmap = new Bitmap(ms);
			image = new ImageElement { Image = bitmap, Size = bitmap.Size };
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsHexDigit(char ch)
	{
		return (ch >= '0' && ch <= '9')
			|| (ch >= 'A' && ch <= 'F')
			|| (ch >= 'a' && ch <= 'f');
	}

	private int ParseControlWord(string rtf, int start, Stack<GroupContext> stack)
	{
		var current = stack.Peek();
		var i = start + 1;
		if (i >= rtf.Length)
			return i;

		var c = rtf[i];

		if (c == '\\' || c == '{' || c == '}')
		{
			_escapedNewlineControlStreak = 0;
			if (!current.IgnoreDestination)
			{
				if (current.Kind == GroupKind.FontTable && current.FontEntryIndex.HasValue)
					current.FlatFontEntryText.Append(c);
				else
					current.Text.Append(c);
			}
			return i + 1;
		}

		if (c == '\'')
		{
			_escapedNewlineControlStreak = 0;
			if (i + 2 < rtf.Length)
			{
				var hex = rtf.Substring(i + 1, 2);
				if (byte.TryParse(hex, NumberStyles.HexNumber, null, out var hexValue))
				{
					if (!current.IgnoreDestination)
					{
						if (current.Kind == GroupKind.FontTable && current.FontEntryIndex.HasValue)
							current.FlatFontEntryText.Append((char)hexValue);
						else
							current.Text.Append((char)hexValue);
					}
					return i + 3;
				}
			}
			return i + 1;
		}

		if (c == '*')
		{
			_escapedNewlineControlStreak = 0;
			current.IgnoreDestination = true;
			return i + 1;
		}

		if (c == '\r' || c == '\n')
		{
			var nextIndex = i + 1;
			if (c == '\r' && i + 1 < rtf.Length && rtf[i + 1] == '\n')
				nextIndex = i + 2;

			if (!current.IgnoreDestination && current.Kind != GroupKind.ColorTable && current.Kind != GroupKind.FontTable)
			{
				var shouldCommitParagraph = _escapedNewlineControlStreak > 0;
				_escapedNewlineControlStreak++;
				if (!shouldCommitParagraph)
					shouldCommitParagraph = !IsEscapedNewlineControlAt(rtf, nextIndex);
				if (shouldCommitParagraph)
					CommitParagraphBreakFromContext(current);
			}

			return nextIndex;
		}

		_escapedNewlineControlStreak = 0;

		if (!char.IsLetter(c))
		{
			if (current.Kind == GroupKind.ColorTable && c == ';')
				CompleteColorEntry(current);
			if (current.Kind == GroupKind.FontTable && c == ';')
				CompleteFlatFontEntry(current);

			if (!current.IgnoreDestination && current.Kind != GroupKind.ColorTable && current.Kind != GroupKind.FontTable)
			{
				switch (c)
				{
					case '~':
						current.Text.Append('\u00A0');
						break;
					case '-':
						current.Text.Append('\u00AD');
						break;
					case '_':
						current.Text.Append('\u2011');
						break;
					default:
						current.Text.Append(c);
						break;
				}
			}
			return i + 1;
		}

		var wordStart = i;
		while (i < rtf.Length && char.IsLetter(rtf[i]))
			i++;
		var word = rtf.Substring(wordStart, i - wordStart);

		var sign = 1;
		if (i < rtf.Length && (rtf[i] == '-' || rtf[i] == '+'))
		{
			sign = rtf[i] == '-' ? -1 : 1;
			i++;
		}

		var value = 0;
		var hasValue = false;
		while (i < rtf.Length && char.IsDigit(rtf[i]))
		{
			hasValue = true;
			value = value * 10 + (rtf[i] - '0');
			i++;
		}
		if (hasValue)
			value *= sign;

		if (i < rtf.Length && rtf[i] == ' ')
			i++;

		if (IsStyleControlWord(word, current))
			FinalizeText(current);

		HandleControlWord(current, word, hasValue ? value : (int?)null);
		if (word == "u" && i < rtf.Length)
		{
			// RTF \u control is followed by one fallback character for legacy readers.
			i++;
		}
		return i;
	}

	private static bool IsEscapedNewlineControlAt(string rtf, int index)
	{
		if (index < 0 || index >= rtf.Length - 1 || rtf[index] != '\\')
			return false;

		var newline = rtf[index + 1];
		return newline == '\r' || newline == '\n';
	}

	private void HandleControlWord(GroupContext context, string word, int? value)
	{
		switch (word)
		{
			case "rtf":
				context.Kind = GroupKind.Document;
				return;
			case "pard":
				context.Kind = GroupKind.Paragraph;
				return;
			case "listtext":
				context.Kind = GroupKind.List;
				return;
			case "listitem":
				context.Kind = GroupKind.ListItem;
				return;
			case "pict":
				context.Kind = GroupKind.Picture;
				return;
			case "fonttbl":
				context.Kind = GroupKind.FontTable;
				return;
			case "colortbl":
				context.Kind = GroupKind.ColorTable;
				return;
			case "f":
				if (!value.HasValue)
					return;
				if (context.Kind == GroupKind.FontTable)
				{
					StartFlatFontEntry(context, value.Value);
					return;
				}
				if (context.Parent?.Kind == GroupKind.FontTable)
				{
					context.Kind = GroupKind.FontEntry;
					context.FontEntryIndex = value.Value;
					context.FontEntryFamilyHint = null;
					return;
				}
				context.Style.FontIndex = value.Value;
				return;
			case "fnil":
			case "froman":
			case "fswiss":
			case "fmodern":
			case "fscript":
			case "fdecor":
			case "ftech":
			case "fbidi":
				if (context.Kind == GroupKind.FontEntry || (context.Kind == GroupKind.FontTable && context.FontEntryIndex.HasValue))
					context.FontEntryFamilyHint = word;
				return;
			case "plain":
				context.Style.Bold = false;
				context.Style.Italic = false;
				context.Style.Underline = false;
				context.Style.Strikethrough = false;
				context.Style.Superscript = false;
				context.Style.Subscript = false;
				context.Style.FontSize = null;
				context.Style.FontIndex = null;
				context.Style.ForegroundColorIndex = null;
				context.Style.BackgroundColorIndex = null;
				return;
			case "fs":
				if (value.HasValue)
					context.Style.FontSize = value.Value / 2f;
				return;
			case "b":
				context.Style.Bold = !value.HasValue || value.Value != 0;
				return;
			case "ab":
				context.Style.Bold = !value.HasValue || value.Value != 0;
				return;
			case "i":
				context.Style.Italic = !value.HasValue || value.Value != 0;
				return;
			case "ai":
				context.Style.Italic = !value.HasValue || value.Value != 0;
				return;
			case "ul":
				context.Style.Underline = !value.HasValue || value.Value != 0;
				return;
			case "ulnone":
				context.Style.Underline = false;
				return;
			case "strike":
				context.Style.Strikethrough = !value.HasValue || value.Value != 0;
				return;
			case "nosupersub":
				context.Style.Superscript = false;
				context.Style.Subscript = false;
				return;
			case "super":
				context.Style.Superscript = true;
				context.Style.Subscript = false;
				return;
			case "sub":
				context.Style.Subscript = true;
				context.Style.Superscript = false;
				return;
			case "cf":
				if (value.HasValue)
					context.Style.ForegroundColorIndex = value.Value;
				return;
			case "highlight":
				if (value.HasValue)
					context.Style.BackgroundColorIndex = value.Value;
				return;
			case "red":
				if (context.Kind == GroupKind.ColorTable && value.HasValue)
				{
					context.ColorRed = ClampColor(value.Value);
					context.HasColorComponent = true;
				}
				return;
			case "green":
				if (context.Kind == GroupKind.ColorTable && value.HasValue)
				{
					context.ColorGreen = ClampColor(value.Value);
					context.HasColorComponent = true;
				}
				return;
			case "blue":
				if (context.Kind == GroupKind.ColorTable && value.HasValue)
				{
					context.ColorBlue = ClampColor(value.Value);
					context.HasColorComponent = true;
				}
				return;
			case "par":
				CommitParagraphBreakFromContext(context);
				return;
			case "line":
				if (!context.IgnoreDestination)
					context.Text.Append(SpecialCharacters.SoftBreakCharacter);
				return;
			case "tab":
				if (!context.IgnoreDestination)
					context.Text.Append(SpecialCharacters.TabCharacter);
				return;
			case "u":
				if (!context.IgnoreDestination && value.HasValue)
					context.Text.Append(unchecked((char)(short)value.Value));
				return;
			case "~":
				if (!context.IgnoreDestination)
					context.Text.Append('\u00A0');
				return;
			default:
				return;
		}
	}

	private static bool IsStyleControlWord(string word, GroupContext context)
	{
		if (context.Kind == GroupKind.ColorTable || context.Kind == GroupKind.FontTable || context.Kind == GroupKind.FontEntry)
			return false;

		return word == "plain"
			|| word == "f"
			|| word == "fs"
			|| word == "b"
			|| word == "ab"
			|| word == "i"
			|| word == "ai"
			|| word == "ul"
			|| word == "ulnone"
			|| word == "strike"
			|| word == "nosupersub"
			|| word == "super"
			|| word == "sub"
			|| word == "cf"
			|| word == "highlight";
	}

	private void CompleteColorEntry(GroupContext context)
	{
		var index = _nextColorTableIndex++;
		if (index > 0 && context.HasColorComponent)
		{
			_colorTable[index] = Color.FromArgb((byte)context.ColorRed, (byte)context.ColorGreen, (byte)context.ColorBlue);
		}
		context.ColorRed = 0;
		context.ColorGreen = 0;
		context.ColorBlue = 0;
		context.HasColorComponent = false;
	}

	private static GroupContext? FindNearestParagraphContext(GroupContext? context)
	{
		var current = context;
		while (current != null)
		{
			if (current.Kind == GroupKind.Paragraph)
				return current;
			current = current.Parent;
		}
		return null;
	}

	private void CommitParagraphBreakFromContext(GroupContext context)
	{
		var target = FindNearestParagraphContext(context);
		if (target == null || target.IgnoreDestination)
			return;

		FlushNestedContentToParagraph(context, target);
		CommitParagraphBreak(target);
	}

	private void FlushNestedContentToParagraph(GroupContext from, GroupContext target)
	{
		var current = from;
		while (current != target)
		{
			FinalizeText(current);
			var parent = current.Parent;
			if (parent == null)
				break;

			if (current.Inlines.Count > 0 || current.Blocks.Count > 0)
			{
				AttachChild(parent, current);
				current.Inlines.Clear();
				current.Blocks.Clear();
				current.Text.Clear();
			}

			current = parent;
		}
	}

	private static int ClampColor(int value)
	{
		if (value < 0)
			return 0;
		if (value > 255)
			return 255;
		return value;
	}
}
