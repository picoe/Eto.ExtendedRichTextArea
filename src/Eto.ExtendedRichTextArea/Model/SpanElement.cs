
using Eto.Drawing;

namespace Eto.ExtendedRichTextArea.Model
{

	public class SpanElement : IInlineElement
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
		
		public IElement? Parent { get; private set; }
		
		IElement? IElement.Parent
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

		public SpanElement? Split(int index)
		{
			if (index >= Length || index <=0)
				return null;
			var text = Text;
			Text = text.Substring(0, index);
			var newSpan = new SpanElement { 
				Start = index,
				Text = text.Substring(index), Attributes = Attributes?.Clone() 
			};
			return newSpan;
		}

		IElement? IElement.Split(int index) => Split(index);

		internal SpanElement WithText(string text)
		{
			if (text == Text)
				return this;
			var span = new SpanElement { Text = text, Attributes = Attributes?.Clone() };
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
			return length;
		}

		public void Recalculate(int index)
		{
			// nothing to recalculate for this one
		}
		
		Attributes? _resolvedAttributes;
		public Font Font => _resolvedAttributes?.Font ?? Document.GetDefaultFont();

        public int GetIndexAt(Chunk chunk, PointF point)
        {
			if (point.X > chunk.Bounds.Right || point.Y > chunk.Bounds.Bottom)
				return -1;
			if (point.X < chunk.Bounds.Left || point.Y < chunk.Bounds.Top)
				return -1;
			var spanX = chunk.Bounds.X;
			var spanLength = Length;
			var font = Font;
			for (int i = 0; i < spanLength; i++)
			{
				var spanSize = font.MeasureString(Text.Substring(i, 1));
				if (point.X < spanX + spanSize.Width / 2)
					return i;
				spanX += spanSize.Width;
			}
			return -1;
        }
		
		public PointF? GetPointAt(Chunk chunk, int start)
		{
			if (start < 0 || start > chunk.Length)
				return null;
			if (start == chunk.Length)
				return new PointF(chunk.Bounds.Right, chunk.Bounds.Y);
			if (start == 0)
				return new PointF(chunk.Bounds.X, chunk.Bounds.Y);
			var text = Text.Substring(chunk.InlineIndex, start);
			var size = Font?.MeasureString(text) ?? SizeF.Empty;
			return new PointF(chunk.Bounds.X + size.Width, chunk.Bounds.Y);
		}

		public IEnumerable<(string text, int start)> EnumerateWords(int start, bool forward)
		{
			var text = Text;
			if (forward)
			{
				int last = -1;
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
			}
			else
			{
				int last = -1;
				for (int i = start; i >= 0; i--)
				{
					if (char.IsWhiteSpace(text[i]))
					{
						if (last != text.Length)
						{
							yield return (text.Substring(last, i - last), i);
							last = text.Length;
						}
						continue;
					}
					if (last == text.Length)
						last = i;
				}
			}
		}

		public void MeasureIfNeeded() => Parent?.MeasureIfNeeded();

		public bool Matches(IInlineElement element)
		{
			if (element is not SpanElement span)
				return false;
			return span.Attributes == Attributes;
		}

		public bool Merge(int index, IInlineElement element)
		{
			if (element is not SpanElement span || index < 0 || index > Length)
				return false;
			if (!Matches(span))
				return false;
			Text = Text.Insert(index, span.Text);
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
				yield return new SpanElement { Start = start, Attributes = Attributes?.Clone(), Text = Text.Substring(0, end) };
				yield break;
			}
			if (end == Length)
			{
				yield return new SpanElement { Start = start, Attributes = Attributes?.Clone(), Text = Text.Substring(start) };
				yield break;
			}
			yield return new SpanElement { Start = start, Attributes = Attributes?.Clone(), Text = Text.Substring(start, end - start) };
		}

		public void Paint(Chunk chunk, Graphics graphics, RectangleF clipBounds)
		{
			graphics.DrawText(_formattedText, chunk.Bounds.Location);
		}

		public SizeF Measure(Attributes defaultAttributes, SizeF availableSize, out float baseline)
		{
			_documentStart = null;
			_formattedText ??= new FormattedText { Text = Text };
			_resolvedAttributes = defaultAttributes.Merge(Attributes, false);
			_resolvedAttributes.Apply(_formattedText);
			
			if (_measureSize == null)
			{
				_measureSize = _formattedText.Measure();
			}
			baseline = Font?.Baseline ?? 0;
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
				yield return new SpanElement { Start = start, Attributes = Attributes?.Clone(), Text = Text.Substring(0, end) };
				yield break;
			}
			if (end == Length)
			{
				yield return new SpanElement { Start = start, Attributes = Attributes?.Clone(), Text = Text.Substring(start) };
				yield break;
			}
			yield return new SpanElement { Start = start, Attributes = Attributes?.Clone(), Text = Text.Substring(start, end - start) };
		}
	}
}
