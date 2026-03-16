using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Linq;

using Eto.Drawing;
using Eto.ExtendedRichTextArea.Model;

namespace Eto.ExtendedRichTextArea.Formats;

internal class HtmlReader
{
	static readonly Regex TokenRegex = new("(?is)<[^>]+>|[^<]+", RegexOptions.Compiled);
	static readonly Regex TagRegex = new("(?is)^<\\s*(/)?\\s*([a-z0-9]+)([^>]*)/?>$", RegexOptions.Compiled);
	static readonly Regex AttrRegex = new("(?is)([a-z0-9:_-]+)\\s*=\\s*(\"([^\"]*)\"|'([^']*)'|([^\\s\"'>]+))", RegexOptions.Compiled);
	static readonly Regex OrphanedAttributeFragmentRegex = new("(?is)^\\s*(?:class|style|lang|id)\\s*=\\s*[^>]+>\\s*$", RegexOptions.Compiled);
	static readonly Regex ClosingInlineTagFallbackRegex = new("(?is)</\\s*(b|strong|i|em|u|s|strike|del|sub|sup)\\b", RegexOptions.Compiled);
	static readonly Regex StyleTagRegex = new("(?is)<style\\b[^>]*>(.*?)</style>", RegexOptions.Compiled);
	static readonly Regex CssRuleRegex = new("(?is)([^{}]+)\\{([^}]*)\\}", RegexOptions.Compiled);

	sealed class CssRule
	{
		public List<CssSelector> Selectors { get; } = new();
		public string Declarations { get; set; } = string.Empty;
	}

	sealed class CssSelector
	{
		public string? Tag { get; set; }
		public string? Id { get; set; }
		public HashSet<string> Classes { get; } = new(StringComparer.OrdinalIgnoreCase);
	}

	sealed class TextStyle
	{
		public bool Bold { get; set; }
		public bool Italic { get; set; }
		public bool Underline { get; set; }
		public bool Strikethrough { get; set; }
		public bool Superscript { get; set; }
		public bool Subscript { get; set; }
		public string? FontFamily { get; set; }
		public float? FontSizePt { get; set; }
		public Color? Foreground { get; set; }
		public Color? Background { get; set; }
		public bool PreserveWhitespace { get; set; }

		public TextStyle Clone()
		{
			return (TextStyle)MemberwiseClone();
		}
	}

	public Document ReadDocument(string html)
	{
		var document = new Document();
		document.BeginEdit();
		ParseHtml(html, document);
		document.EndEdit();
		return document;
	}

	void ParseHtml(string html, Document document)
	{
		var styleRules = ExtractStylesheetRules(html);
		var cleaned = RemoveIgnoredBlocks(html);
		var topBlocks = new List<IBlockElement>();
		var paragraph = (ParagraphElement?)null;
		var list = (ListElement?)null;
		var listTypeStack = new Stack<ListType>();
		var currentListLevel = -1;
		var currentListItem = (ListItemElement?)null;
		var styleStack = new Stack<TextStyle>();
		styleStack.Push(new TextStyle());
		var lastClosedInlineTag = string.Empty;
		var preserveWhitespaceParagraphs = new HashSet<ParagraphElement>();

		foreach (Match tokenMatch in TokenRegex.Matches(cleaned))
		{
			var token = tokenMatch.Value;
			if (string.IsNullOrEmpty(token))
				continue;

			if (token[0] == '<')
			{
				var rawClosedInlineTag = TryGetFallbackClosingInlineTag(token);
				if (!string.IsNullOrEmpty(rawClosedInlineTag))
					lastClosedInlineTag = rawClosedInlineTag;

				if (!TryParseTag(token, out var isEnd, out var tagName, out var attrs, out var isSelfClosing))
				{
					var fallbackClosedTag = TryGetFallbackClosingInlineTag(token);
					if (!string.IsNullOrEmpty(fallbackClosedTag))
						lastClosedInlineTag = fallbackClosedTag;
					continue;
				}

				if (!isEnd)
				{
					lastClosedInlineTag = string.Empty;
					if (tagName == "ul" || tagName == "ol")
					{
						EnsureParagraphClosed(ref paragraph, topBlocks);
						var newListType = tagName == "ol" ? ListType.Ordered : ListType.Unordered;
						if (list == null)
						{
							list = new ListElement { Type = newListType };
							topBlocks.Add(list);
						}
						listTypeStack.Push(newListType);
						currentListLevel = listTypeStack.Count - 1;
					}
					else if (tagName == "li")
					{
						if (list == null)
						{
							list = new ListElement();
							topBlocks.Add(list);
						}
						currentListItem = new ListItemElement { Level = Math.Max(0, currentListLevel) };
						list.Add(currentListItem);
					}
					else if (tagName == "p")
					{
						if (currentListItem != null)
						{
							if (currentListItem.Count > 0)
								AddInline(currentListItem, new TextElement { Text = ((char)SpecialCharacters.SoftBreakCharacter).ToString() });
						}
						else
						{
							EnsureParagraphClosed(ref paragraph, topBlocks);
							paragraph = new ParagraphElement();
							topBlocks.Add(paragraph);
						}
					}
					else if (tagName == "div")
					{
						if (currentListItem != null)
						{
							if (currentListItem.Count > 0)
								AddInline(currentListItem, new TextElement { Text = ((char)SpecialCharacters.SoftBreakCharacter).ToString() });
						}
						else
						{
							EnsureParagraphClosed(ref paragraph, topBlocks);
							// Lazily create paragraph when actual content arrives, so wrapper divs don't create empty lines.
						}
					}
					else if (tagName == "br")
					{
						var target = EnsureTargetParagraph(ref paragraph, currentListItem, topBlocks);
						// Clipboard HTML often represents blank lines as <div><br></div>.
						// If the block is otherwise empty, keep it as an empty paragraph instead of
						// adding a soft break (which would create an extra blank line with paragraph separators).
						if (target.Count > 0)
							AddInline(target, new TextElement { Text = ((char)SpecialCharacters.SoftBreakCharacter).ToString() });
					}
					else if (tagName == "img")
					{
						var target = EnsureTargetParagraph(ref paragraph, currentListItem, topBlocks);
						if (TryCreateImage(attrs, out var image))
							AddInline(target, image);
					}

					var nextStyle = styleStack.Peek().Clone();
					ApplyStylesheetStyle(nextStyle, tagName, attrs, styleRules);
					ApplyTagStyle(nextStyle, tagName, attrs);
					styleStack.Push(nextStyle);
				}
				else
				{
					lastClosedInlineTag = tagName;
					if (styleStack.Count > 0)
						ResetClosingTagStyle(styleStack.Peek(), tagName);

					if (tagName == "li")
					{
						currentListItem = null;
					}
					else if (tagName == "ul" || tagName == "ol")
					{
						if (listTypeStack.Count > 0)
							listTypeStack.Pop();
						currentListLevel = listTypeStack.Count - 1;
						if (listTypeStack.Count == 0)
							list = null;
					}
					else if ((tagName == "p" || tagName == "div") && currentListItem == null)
					{
						paragraph = null;
					}

					if (styleStack.Count > 1)
						styleStack.Pop();
				}

				if (isSelfClosing && styleStack.Count > 1)
					styleStack.Pop();
				continue;
			}

			var currentStyle = styleStack.Peek();
			var text = WebUtility.HtmlDecode(token);
			if (!currentStyle.PreserveWhitespace)
				text = NormalizeWhitespace(text);
			if (IsOrphanedAttributeFragment(text))
				continue;
			if (string.IsNullOrEmpty(text))
				continue;
			// Ignore inter-tag formatting whitespace at block level (e.g. pretty-printed
			// clipboard HTML with wrapper div + white-space: pre), otherwise it creates
			// phantom paragraphs/newlines. Keep pure-space tokens, since they can be
			// real indentation content inside the first inline span of a block.
			if (string.IsNullOrWhiteSpace(text)
				&& paragraph == null
				&& currentListItem == null
				&& (!currentStyle.PreserveWhitespace || text.Contains('\n') || text.Contains('\r')))
				continue;

			var targetParagraph = EnsureTargetParagraph(ref paragraph, currentListItem, topBlocks);
			if (currentStyle.PreserveWhitespace)
				preserveWhitespaceParagraphs.Add(targetParagraph);

			currentStyle = currentStyle.Clone();
			if (!string.IsNullOrEmpty(lastClosedInlineTag))
				ResetClosingTagStyle(currentStyle, lastClosedInlineTag);
			var attributes = CreateAttributes(currentStyle);
			ApplyImmediatePostCloseStyleReset(lastClosedInlineTag, ref attributes);
			var textElement = new TextElement
			{
				Text = text,
				Attributes = attributes
			};
			AddInline(targetParagraph, textElement);
			lastClosedInlineTag = string.Empty;
		}

		TrimBlockWhitespace(topBlocks, preserveWhitespaceParagraphs);
		foreach (var block in topBlocks)
			document.Add(block);
	}

	static void AddInline(ParagraphElement paragraph, IInlineElement inline)
	{
		paragraph.Add(inline);
	}

	static ParagraphElement EnsureTargetParagraph(ref ParagraphElement? paragraph, ListItemElement? currentListItem, List<IBlockElement> topBlocks)
	{
		if (currentListItem != null)
			return currentListItem;
		if (paragraph == null)
		{
			paragraph = new ParagraphElement();
			topBlocks.Add(paragraph);
		}
		return paragraph;
	}

	static void EnsureParagraphClosed(ref ParagraphElement? paragraph, List<IBlockElement> _)
	{
		paragraph = null;
	}

	static void TrimBlockWhitespace(List<IBlockElement> blocks, HashSet<ParagraphElement> preserveWhitespaceParagraphs)
	{
		foreach (var block in blocks)
		{
			if (block is ParagraphElement paragraph)
			{
				TrimParagraphWhitespace(paragraph, preserveWhitespaceParagraphs.Contains(paragraph));
			}
			else if (block is ListElement list)
			{
				foreach (var item in list)
					TrimParagraphWhitespace(item, preserveWhitespaceParagraphs.Contains(item));
			}
		}
	}

	static void TrimParagraphWhitespace(ParagraphElement paragraph, bool preserveWhitespace)
	{
		if (preserveWhitespace)
			return;

		// Trim leading spaces from first text nodes (tabs are meaningful indentation).
		while (paragraph.Count > 0 && paragraph[0] is TextElement first)
		{
			var text = first.Text;
			var leadingWhitespaceLength = 0;
			while (leadingWhitespaceLength < text.Length && text[leadingWhitespaceLength] == ' ')
				leadingWhitespaceLength++;
			if (leadingWhitespaceLength <= 0)
				break;
			if (leadingWhitespaceLength >= text.Length)
			{
				paragraph.RemoveAt(0);
				continue;
			}
			first.RemoveAt(0, leadingWhitespaceLength);
			break;
		}

		// Trim trailing spaces from last text nodes (tabs are meaningful content).
		while (paragraph.Count > 0 && paragraph[paragraph.Count - 1] is TextElement last)
		{
			var text = last.Text;
			var trailingWhitespaceLength = 0;
			for (var i = text.Length - 1; i >= 0 && text[i] == ' '; i--)
				trailingWhitespaceLength++;
			if (trailingWhitespaceLength <= 0)
				break;
			if (trailingWhitespaceLength >= text.Length)
			{
				paragraph.RemoveAt(paragraph.Count - 1);
				continue;
			}
			last.RemoveAt(text.Length - trailingWhitespaceLength, trailingWhitespaceLength);
			break;
		}
	}

	static string RemoveIgnoredBlocks(string html)
	{
		var result = Regex.Replace(html, "(?is)<!--.*?-->", string.Empty);
		result = Regex.Replace(result, "(?is)<script\\b.*?</script>", string.Empty);
		result = StyleTagRegex.Replace(result, string.Empty);
		return result;
	}

	static List<CssRule> ExtractStylesheetRules(string html)
	{
		var rules = new List<CssRule>();
		foreach (Match match in StyleTagRegex.Matches(html))
		{
			if (!match.Success || match.Groups.Count < 2)
				continue;

			var css = match.Groups[1].Value;
			css = Regex.Replace(css, "(?is)<!--|-->", string.Empty);
			css = Regex.Replace(css, "(?is)/\\*.*?\\*/", string.Empty);

			foreach (Match ruleMatch in CssRuleRegex.Matches(css))
			{
				if (!ruleMatch.Success || ruleMatch.Groups.Count < 3)
					continue;

				var selectorText = ruleMatch.Groups[1].Value.Trim();
				var declarationText = ruleMatch.Groups[2].Value.Trim();
				if (string.IsNullOrEmpty(selectorText) || string.IsNullOrEmpty(declarationText))
					continue;
				if (selectorText.StartsWith("@", StringComparison.Ordinal))
					continue;

				var rule = new CssRule { Declarations = declarationText };
				foreach (var selectorPart in selectorText.Split(','))
				{
					if (TryParseSelector(selectorPart.Trim(), out var selector))
						rule.Selectors.Add(selector);
				}

				if (rule.Selectors.Count > 0)
					rules.Add(rule);
			}
		}
		return rules;
	}

	static bool TryParseSelector(string selectorText, out CssSelector selector)
	{
		selector = new CssSelector();
		if (string.IsNullOrWhiteSpace(selectorText))
			return false;
		if (selectorText.IndexOfAny(new[] { ' ', '>', '+', '~', '[' }) >= 0)
			return false;

		var raw = selectorText;
		var pseudoIndex = raw.IndexOf(':');
		if (pseudoIndex >= 0)
			raw = raw.Substring(0, pseudoIndex);

		var i = 0;
		if (i < raw.Length && (char.IsLetter(raw[i]) || raw[i] == '*'))
		{
			var start = i;
			i++;
			while (i < raw.Length && (char.IsLetterOrDigit(raw[i]) || raw[i] == '-' || raw[i] == '_'))
				i++;
			var tag = raw.Substring(start, i - start);
			if (tag != "*")
				selector.Tag = tag.ToLowerInvariant();
		}

		while (i < raw.Length)
		{
			if (raw[i] == '.')
			{
				i++;
				var start = i;
				while (i < raw.Length && (char.IsLetterOrDigit(raw[i]) || raw[i] == '-' || raw[i] == '_'))
					i++;
				if (i > start)
					selector.Classes.Add(raw.Substring(start, i - start));
				continue;
			}

			if (raw[i] == '#')
			{
				i++;
				var start = i;
				while (i < raw.Length && (char.IsLetterOrDigit(raw[i]) || raw[i] == '-' || raw[i] == '_'))
					i++;
				if (i > start)
					selector.Id = raw.Substring(start, i - start);
				continue;
			}

			// Unsupported selector syntax.
			return false;
		}

		return selector.Tag != null || selector.Id != null || selector.Classes.Count > 0;
	}

	static void ApplyStylesheetStyle(TextStyle style, string tagName, Dictionary<string, string> attrs, List<CssRule> rules)
	{
		if (rules.Count == 0)
			return;

		var id = attrs.TryGetValue("id", out var idValue) ? idValue : null;
		var classSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (attrs.TryGetValue("class", out var classValue))
		{
			foreach (var className in classValue.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
				classSet.Add(className);
		}

		foreach (var rule in rules)
		{
			if (!MatchesAnySelector(rule.Selectors, tagName, id, classSet))
				continue;
			ApplyCssStyle(style, rule.Declarations);
		}
	}

	static bool MatchesAnySelector(List<CssSelector> selectors, string tagName, string? id, HashSet<string> classes)
	{
		foreach (var selector in selectors)
		{
			if (MatchesSelector(selector, tagName, id, classes))
				return true;
		}
		return false;
	}

	static bool MatchesSelector(CssSelector selector, string tagName, string? id, HashSet<string> classes)
	{
		if (selector.Tag != null && !selector.Tag.Equals(tagName, StringComparison.OrdinalIgnoreCase))
			return false;
		if (selector.Id != null && !selector.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
			return false;
		foreach (var className in selector.Classes)
		{
			if (!classes.Contains(className))
				return false;
		}
		return true;
	}

	static string NormalizeWhitespace(string text)
	{
		text = text.Replace('\u00A0', ' ');
		// Collapse HTML formatting whitespace but preserve literal tab characters.
		text = Regex.Replace(text, "[ \\r\\n\\f\\v]+", " ");
		return text;
	}

	static bool IsOrphanedAttributeFragment(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
			return false;
		return OrphanedAttributeFragmentRegex.IsMatch(text);
	}

	static void ResetClosingTagStyle(TextStyle style, string tagName)
	{
		switch (tagName)
		{
			case "b":
			case "strong":
				style.Bold = false;
				break;
			case "i":
			case "em":
				style.Italic = false;
				break;
			case "u":
				style.Underline = false;
				break;
			case "s":
			case "strike":
			case "del":
				style.Strikethrough = false;
				break;
			case "sub":
				style.Subscript = false;
				break;
			case "sup":
				style.Superscript = false;
				break;
		}
	}

	static string TryGetFallbackClosingInlineTag(string token)
	{
		if (string.IsNullOrEmpty(token))
			return string.Empty;
		var match = ClosingInlineTagFallbackRegex.Match(token);
		return match.Success && match.Groups.Count > 1 ? match.Groups[1].Value.ToLowerInvariant() : string.Empty;
	}

	static void ApplyImmediatePostCloseStyleReset(string closedTag, ref Attributes? attributes)
	{
		if (string.IsNullOrEmpty(closedTag))
			return;

		switch (closedTag)
		{
			case "b":
			case "strong":
				attributes ??= new Attributes();
				attributes.Bold = false;
				break;
			case "i":
			case "em":
				attributes ??= new Attributes();
				attributes.Italic = false;
				break;
		}
	}

	static bool TryParseTag(string token, out bool isEnd, out string tagName, out Dictionary<string, string> attrs, out bool isSelfClosing)
	{
		isEnd = false;
		isSelfClosing = false;
		tagName = string.Empty;
		attrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		var match = TagRegex.Match(token);
		if (!match.Success)
			return false;

		isEnd = !string.IsNullOrEmpty(match.Groups[1].Value);
		tagName = match.Groups[2].Value.ToLowerInvariant();
		var attrText = match.Groups[3].Value;
		isSelfClosing = token.EndsWith("/>", StringComparison.Ordinal);

		foreach (Match attr in AttrRegex.Matches(attrText))
		{
			var key = attr.Groups[1].Value.ToLowerInvariant();
			var value = attr.Groups[3].Success
				? attr.Groups[3].Value
				: attr.Groups[4].Success
					? attr.Groups[4].Value
					: attr.Groups[5].Value;
			attrs[key] = WebUtility.HtmlDecode(value);
		}

		return true;
	}

	static void ApplyTagStyle(TextStyle style, string tagName, Dictionary<string, string> attrs)
	{
		switch (tagName)
		{
			case "pre":
				style.PreserveWhitespace = true;
				break;
			case "b":
			case "strong":
				style.Bold = true;
				break;
			case "i":
			case "em":
				style.Italic = true;
				break;
			case "u":
				style.Underline = true;
				break;
			case "s":
			case "strike":
			case "del":
				style.Strikethrough = true;
				break;
			case "sub":
				style.Subscript = true;
				style.Superscript = false;
				break;
			case "sup":
				style.Superscript = true;
				style.Subscript = false;
				break;
			case "font":
				if (attrs.TryGetValue("face", out var face))
					style.FontFamily = face;
				if (attrs.TryGetValue("size", out var fontSize) && float.TryParse(fontSize, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedSize))
					style.FontSizePt = parsedSize;
				if (attrs.TryGetValue("color", out var color))
					style.Foreground = ParseColor(color);
				break;
		}

		if (attrs.TryGetValue("style", out var styleText))
			ApplyCssStyle(style, styleText);
	}

	static void ApplyCssStyle(TextStyle style, string css)
	{
		var parts = css.Split(';');
		foreach (var part in parts)
		{
#if NET
			var kv = part.Split(':', 2);
#else
			var kv = part.Split(new[] { ':' }, 2);
#endif
			if (kv.Length != 2)
				continue;
			var key = kv[0].Trim().ToLowerInvariant();
			var value = kv[1].Trim();
			switch (key)
			{
				case "font-family":
					style.FontFamily = value.Trim('\'', '"');
					break;
				case "font-size":
					if (TryParseCssSize(value, out var pt))
						style.FontSizePt = pt;
					break;
				case "font-weight":
					style.Bold = value.Equals("bold", StringComparison.OrdinalIgnoreCase)
						|| int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var weight) && weight >= 600;
					break;
				case "font-style":
					style.Italic = value.Equals("italic", StringComparison.OrdinalIgnoreCase) || value.Equals("oblique", StringComparison.OrdinalIgnoreCase);
					break;
				case "color":
					style.Foreground = ParseColor(value);
					break;
				case "background":
				case "background-color":
					style.Background = ParseColor(value);
					break;
				case "text-decoration":
					if (value.IndexOf("underline", StringComparison.OrdinalIgnoreCase) >= 0)
						style.Underline = true;
					if (value.IndexOf("line-through", StringComparison.OrdinalIgnoreCase) >= 0)
						style.Strikethrough = true;
					break;
				case "vertical-align":
					if (value.Equals("super", StringComparison.OrdinalIgnoreCase))
					{
						style.Superscript = true;
						style.Subscript = false;
					}
					else if (value.Equals("sub", StringComparison.OrdinalIgnoreCase))
					{
						style.Subscript = true;
						style.Superscript = false;
					}
					break;
				case "white-space":
					style.PreserveWhitespace = value.Equals("pre", StringComparison.OrdinalIgnoreCase)
						|| value.Equals("pre-wrap", StringComparison.OrdinalIgnoreCase)
						|| value.Equals("break-spaces", StringComparison.OrdinalIgnoreCase);
					break;
			}
		}
	}

	static bool TryParseCssSize(string cssValue, out float points)
	{
		points = 0;
		cssValue = cssValue.Trim();
		if (cssValue.EndsWith("pt", StringComparison.OrdinalIgnoreCase))
			cssValue = cssValue.Substring(0, cssValue.Length - 2);
		else if (cssValue.EndsWith("px", StringComparison.OrdinalIgnoreCase))
		{
			var px = cssValue.Substring(0, cssValue.Length - 2);
			if (float.TryParse(px, NumberStyles.Float, CultureInfo.InvariantCulture, out var pxValue))
			{
				points = pxValue * 72f / 96f;
				return true;
			}
			return false;
		}

		return float.TryParse(cssValue, NumberStyles.Float, CultureInfo.InvariantCulture, out points);
	}

	static Color? ParseColor(string value)
	{
		try
		{
			return Color.Parse(value.Trim());
		}
		catch
		{
			return null;
		}
	}

	static Attributes? CreateAttributes(TextStyle style)
	{
		var attributes = new Attributes();
		var hasValue = false;

		if (!string.IsNullOrWhiteSpace(style.FontFamily))
		{
			try
			{
				attributes.Family = new FontFamily(style.FontFamily);
				hasValue = true;
			}
			catch
			{
				// Ignore unavailable font families.
			}
		}
		if (style.FontSizePt.HasValue && style.FontSizePt.Value > 0)
		{
			attributes.Size = style.FontSizePt.Value;
			hasValue = true;
		}

		if (style.Bold || style.Italic)
		{
			var family = attributes.Family ?? Document.GetDefaultFont().Family;
			var fontStyle = FontStyle.None;
			if (style.Bold)
				fontStyle |= FontStyle.Bold;
			if (style.Italic)
				fontStyle |= FontStyle.Italic;
			var typeface = family.Typefaces.FirstOrDefault(r => r.FontStyle == fontStyle)
				?? family.Typefaces.FirstOrDefault(r => r.FontStyle.HasFlag(fontStyle));
			if (typeface != null)
			{
				attributes.Typeface = typeface;
				hasValue = true;
			}
		}

		if (style.Foreground is Color fg)
		{
			attributes.Foreground = new SolidBrush(fg);
			hasValue = true;
		}
		if (style.Background is Color bg)
		{
			attributes.Background = new SolidBrush(bg);
			hasValue = true;
		}
		if (style.Underline)
		{
			attributes.Underline = true;
			hasValue = true;
		}
		if (style.Strikethrough)
		{
			attributes.Strikethrough = true;
			hasValue = true;
		}
		if (style.Superscript)
		{
			attributes.Superscript = true;
			hasValue = true;
		}
		else if (style.Subscript)
		{
			attributes.Subscript = true;
			hasValue = true;
		}

		return hasValue ? attributes : null;
	}

	static bool TryCreateImage(Dictionary<string, string> attrs, out ImageElement imageElement)
	{
		imageElement = null!;
		if (!attrs.TryGetValue("src", out var src))
			return false;

		byte[]? bytes = null;
		if (src.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
		{
			var comma = src.IndexOf(',');
			if (comma > 0)
			{
				var meta = src.Substring(5, comma - 5);
				var data = src.Substring(comma + 1);
				if (meta.IndexOf(";base64", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					try
					{
						bytes = Convert.FromBase64String(data);
					}
					catch
					{
						bytes = null;
					}
				}
			}
		}

		if (bytes == null || bytes.Length == 0)
			return false;

		try
		{
			using var ms = new MemoryStream(bytes);
			var bitmap = new Bitmap(ms);
			imageElement = new ImageElement
			{
				Image = bitmap,
				Size = bitmap.Size
			};
			return true;
		}
		catch
		{
			return false;
		}
	}
}
