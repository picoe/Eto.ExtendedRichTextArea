
using Eto.Drawing;
using Eto.Forms;

namespace Eto.ExtendedRichTextArea.Model;

public class TextElement : IInlineElement
{
	FormattedText? _formattedText;
	SizeF? _measureSize;
	string? _text;
	Attributes? _attributes;

	/// <summary>
	/// Gets or sets an application-defined object associated with this element.
	/// </summary>
	public object? Tag { get; set; }

	public Attributes? Attributes
	{
		get => _attributes;
		set
		{
			_attributes = value;
			_measureSize = null;
		}
	}
	
	public Attributes ActualAttributes => _resolvedAttributes ?? Attributes ?? new Attributes();

	public IBlockElement? Parent { get; private set; }

	IBlockElement? IElement.Parent
	{
		get => Parent;
		set => Parent = value;
	}

	public int Start { get; set; }
	public int Length => Text.Length;
	public int End => Start + Length;

	int? _documentStart;
	public int DocumentStart => _documentStart ??= Start + Parent?.DocumentStart ?? 0;

	public string Text
	{
		get => _text ?? string.Empty;
		set
		{
			_text = value;
			if (_formattedText != null)
				_formattedText.Text = value;
			_measureSize = null;
		}
	}

	public TextElement? Split(int start)
	{
		if (start >= Length || start < 0)
			return null;
		var text = Text;
		Text = text.Substring(0, start);
		var newSpan = new TextElement
		{
			Start = Start + start,
			Text = text.Substring(start),
			Attributes = Attributes?.Clone()
		};
		Parent?.Adjust(Parent.IndexOf(this), -newSpan.Length);

		return newSpan;
	}

	IElement? IElement.Split(int index) => Split(index);

	internal TextElement WithText(string text)
	{
		if (text == Text)
			return this;
		var span = new TextElement { Text = text, Attributes = Attributes?.Clone() };
		return span;
	}

	public int RemoveAt(int index, int length)
	{
		var text = Text;
		if (index < 0 || index >= text.Length)
			return 0;
		if (index + length > text.Length)
			length = text.Length - index;
		Text = text.Remove(index, length);
		Parent?.Adjust(Parent.IndexOf(this), -length);
		return length;
	}

	Attributes? _resolvedAttributes;
	public Font Font => _resolvedAttributes?.Font ?? Document.GetDefaultFont();

	public int GetIndexAt(Chunk chunk, PointF point)
	{
		if (point.X < chunk.Bounds.Left || point.X > chunk.Bounds.Right)
			return -1;
		// if ( || point.Y < chunk.Bounds.Top)
		// 	return -1;
		var spanX = chunk.Bounds.X;
		var spanLength = chunk.Length;
		var font = Font;
		for (int i = 0; i < spanLength; i++)
		{
			var spanSize = font.MeasureString(Text.Substring(i + chunk.InlineStart, 1));
			if (point.X < spanX + spanSize.Width / 2)
				return i;
			spanX += spanSize.Width;
		}
		return chunk.Length;
	}

	public PointF? GetPointAt(Chunk chunk, int start)
	{
		if (start < 0 || start > chunk.Length)
			return null;
		if (start == chunk.Length)
			return new PointF(chunk.Bounds.Right, chunk.Bounds.Y);
		if (start == 0)
			return new PointF(chunk.Bounds.X, chunk.Bounds.Y);
		var text = Text.Substring(chunk.InlineStart, start);
		var size = Font?.MeasureString(text) ?? SizeF.Empty;
		return new PointF(chunk.Bounds.X + size.Width, chunk.Bounds.Y);
	}

	public IEnumerable<(string text, int start)> EnumerateWords(int start, bool forward)
	{
		if (start < 0)
			throw new ArgumentOutOfRangeException(nameof(start), start, "Start must be greater than or equal to 0");
		if (start > Length)
			throw new ArgumentOutOfRangeException(nameof(start), start, "Start must be less than or equal to the length");

		var text = Text;
		if (forward)
		{
			int last = -1;
			// try going backwards first, the start might not be at the beginning of the word.
			if (start > 0)
			{
				for (int i = start - 1; i >= 0; i--)
				{
					if (char.IsWhiteSpace(text[i]))
						break;
					last = i;
				}
			}

			for (int i = start; i < text.Length; i++)
			{
				if (char.IsWhiteSpace(text[i]))
				{
					if (last != -1)
					{
						yield return (text.Substring(last, i - last), last);
						last = -1;
					}
					continue;
				}
				if (last == -1)
					last = i;
			}
			if (last != -1)
				yield return (text.Substring(last), last);
		}
		else
		{
			int last = -1;
			// try going forwards first, the start might not be at the end of the word.
			if (start < text.Length)
			{
				for (int i = start; i < text.Length; i++)
				{
					if (char.IsWhiteSpace(text[i]))
						break;
					last = i;
				}
			}


			for (int i = start - 1; i >= 0; i--)
			{
				if (char.IsWhiteSpace(text[i]))
				{
					if (last != -1)
					{
						yield return (text.Substring(i + 1, last - i), i + 1);
						last = -1;
					}
					continue;
				}
				if (last == -1)
					last = i;
			}
			if (last != -1 && last > 0)
				yield return (text.Substring(0, last + 1), 0);
		}
	}

	public void MeasureIfNeeded() => Parent?.MeasureIfNeeded();

	public bool Matches(IInlineElement element)
	{
		if (element is not TextElement span)
			return false;

		return span.Attributes == (_resolvedAttributes ?? Attributes);
	}

	public bool Merge(int index, IInlineElement element)
	{
		if (element is not TextElement span || index < 0 || index > Length)
			return false;
		if ((span.Attributes != null || span.Parent != null) && !Matches(span))
			return false;
		Text = Text.Insert(index, span.Text);
		Parent?.Adjust(Parent.IndexOf(this), span.Text.Length);
		return true;
	}

	public IEnumerable<IInlineElement> EnumerateInlines(int start, int end, bool trim)
	{
		if (end < start)
			throw new ArgumentOutOfRangeException(nameof(end), "End must be greater than or equal to start");

		if (start < 0)
		{
			end += start;
			start = 0;
		}
		if (start >= Length)
			yield break;
		if (end > Length)
			end = Length;
		if (end < 0)
			yield break;
		if ((start == 0 && end == Length) || !trim)
		{
			yield return this;
			yield break;
		}
		if (start == 0)
		{
			yield return new TextElement { Start = start, Attributes = Attributes?.Clone(), Text = Text.Substring(0, end) };
			yield break;
		}
		if (end == Length)
		{
			yield return new TextElement { Start = start, Attributes = Attributes?.Clone(), Text = Text.Substring(start) };
			yield break;
		}
		yield return new TextElement { Start = start, Attributes = Attributes?.Clone(), Text = Text.Substring(start, end - start) };
	}

	public void Paint(Line line, Chunk chunk, Graphics graphics, RectangleF clipBounds)
	{
		if (_resolvedAttributes == null || _formattedText == null)
			return;

		var location = chunk.Bounds.Location;
		location.Y += line.Baseline - _resolvedAttributes.Baseline ?? line.Baseline;

		location.Y += _resolvedAttributes.LineOffset ?? 0;

		var start = chunk.InlineStart;

		void DrawBackground(Attributes attributes, float? left, float? right)
		{
			if (attributes.Background == null)
				return;
			var bounds = chunk.Bounds;
			if (left != null)
				bounds.Left = left.Value;
			if (right != null)
				bounds.Right = right.Value;

			bounds.Y = line.Bounds.Y;
			bounds.Height = line.Bounds.Height;
			graphics.FillRectangle(attributes.Background, bounds);
		}

		void DrawText(Attributes attributes, int end)
		{
			var text = Text.Substring(start, end - start);
			// special case tab character
			if (text == SpecialCharacters.TabCharacterString)
			{
				DrawBackground(attributes, null, null);
				return;
			}
			using var formattedText = new FormattedText { Text = text };
			attributes.Apply(formattedText);
			var width = formattedText.Measure().Width;
			DrawBackground(attributes, location.X, location.X + width);

			graphics.DrawText(formattedText, location);
			location.X += width;
			start += text.Length;
		}

		// TODO: Check performance of this?
		var doc = this.GetDocument();
		if (doc != null)
		{
			doc.TriggerOverrideAttributes(line, chunk, _resolvedAttributes, out var newAttributes);
			if (newAttributes?.Count > 0)
			{
				// need to draw in parts
				var docStart = DocumentStart + chunk.InlineStart;
				var docEnd = docStart + chunk.Length;
				foreach (var attr in newAttributes)
				{
					// skip if it's outside this chunk range
					if (attr.End < docStart)
						continue;
					if (attr.Start > docEnd)
						break;
					// ensure it's in range
					var attrStart = Math.Max(chunk.InlineStart, attr.Start - docStart + chunk.InlineStart);
					var attrEnd = Math.Min(attr.End - docStart + chunk.InlineStart, chunk.InlineEnd);
					if (attrStart > start)
					{
						// draw first part without override
						DrawText(_resolvedAttributes, attrStart);
					}

					if (attrEnd > start)
					{
						var attributes = _resolvedAttributes.Merge(attr.Attributes, false);
						DrawText(attributes, attrEnd);
					}
				}
			}
		}

		if (start == 0 && chunk.Length == Length)
		{
			// draw whole thing.
			DrawBackground(_resolvedAttributes, null, null);
			graphics.DrawText(_formattedText, location);
		}
		else if (start < chunk.InlineEnd)
		{
			// draw last part without override
			DrawText(_resolvedAttributes, chunk.InlineEnd);
		}
	}

	public SizeF Measure(Attributes defaultAttributes, SizeF availableSize, int start, int length, out float baseline)
	{
		_documentStart = null;
		_formattedText ??= new FormattedText { Text = Text };
		_resolvedAttributes = defaultAttributes.Merge(Attributes, false);
		_resolvedAttributes.Apply(_formattedText);
		baseline = _resolvedAttributes.Baseline ?? 0;
		var lineHeight = _resolvedAttributes.LineHeight ?? 0;
		

		if (start > 0 || length < Text.Length)
		{
			// we need to create a new formatted text for the substring
			_formattedText.Text = Text.Substring(start, length);
			var size = _formattedText.Measure();
			size.Height = lineHeight;
			_formattedText.Text = Text; // reset to full text
			return size;
		}

		if (_measureSize == null)
		{
			var measureSize = _formattedText.Measure();
			measureSize.Height = lineHeight;
			_measureSize = measureSize;
		}
		return _measureSize.Value;
	}

	public IEnumerable<IElement> Enumerate(int start, int end, bool trimInlines)
	{
		if (end < start)
			throw new ArgumentOutOfRangeException(nameof(end), "End must be greater than or equal to start");

		if (start < 0)
		{
			end += start;
			start = 0;
		}
		if (start >= Length)
			yield break;
		if (end > Length)
			end = Length;
		if (end <= 0)
			yield break;
		if ((start == 0 && end == Length) || !trimInlines)
		{
			yield return this;
			yield break;
		}
		if (start == 0)
		{
			yield return new TextElement { Start = start, Attributes = Attributes?.Clone(), Text = Text.Substring(0, end) };
			yield break;
		}
		if (end == Length)
		{
			yield return new TextElement { Start = start, Attributes = Attributes?.Clone(), Text = Text.Substring(start) };
			yield break;
		}
		yield return new TextElement { Start = start, Attributes = Attributes?.Clone(), Text = Text.Substring(start, end - start) };
	}

	public bool InsertAt(int start, IElement element)
	{
		if (element is not TextElement span)
			return false;
		if (start < 0 || start > Length)
			throw new ArgumentOutOfRangeException(nameof(start), "Start must be between 0 and the length of the element");

		if (span.Attributes != Attributes)
			return false;

		Parent?.Adjust(Parent.IndexOf(this), span.Length);
		if (start == Length)
		{
			Text += span.Text;
			return true;
		}
		if (start == 0)
		{
			Text = span.Text + Text;
			return true;
		}
		Text = Text.Insert(start, span.Text);
		return true;
	}

	public object Clone()
	{
		return new TextElement
		{
			Start = Start,
			Attributes = Attributes?.Clone(),
			Text = Text,
			Tag = Tag
		};
	}

	public void OnKeyDown(int start, int end, KeyEventArgs args)
	{
	}
}
