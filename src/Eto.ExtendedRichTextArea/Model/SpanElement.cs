
using Eto.Drawing;

namespace Eto.ExtendedRichTextArea.Model
{
	public class Attributes
	{
		public Font? Font { get; set; }
		public Brush? Brush { get; set; }
		public bool Underline { get; set; }
		public bool Strikethrough { get; set; }
		public float Offset { get; set; }
		
		public Attributes Clone()
		{
			return new Attributes
			{
				Font = Font,
				Brush = Brush,
				Underline = Underline,
				Strikethrough = Strikethrough,
				Offset = Offset
			};
		}
		
		// override object.Equals
		public override bool Equals(object? obj)
		{
			//
			// See the full list of guidelines at
			//   http://go.microsoft.com/fwlink/?LinkID=85237
			// and also the guidance for operator== at
			//   http://go.microsoft.com/fwlink/?LinkId=85238
			//
			if (obj == null || obj is not Attributes other)
				return false;

			if (ReferenceEquals(this, obj))
				return true;
				
			return Font == other.Font 
				&& Brush == other.Brush 
				&& Underline == other.Underline 
				&& Strikethrough == other.Strikethrough 
				&& Offset == other.Offset;
		}
		
		// override object.GetHashCode
		public override int GetHashCode()
		{
			return HashCode.Combine(Font, Brush, Underline, Strikethrough, Offset);
		}
	}

	public interface IInlineElement : IElement
	{
		bool Matches(IInlineElement element);
		bool Merge(int index, IInlineElement element);
		void Paint(Chunk chunk, Graphics graphics, RectangleF clipBounds);
		PointF? GetPointAt(Chunk chunk, int start);
		int GetIndexAt(Chunk chunk, PointF point);
		SizeF Measure(SizeF availableSize, out float baseline);
	}
	
	public class SpanElement : IInlineElement
	{
		readonly FormattedText _text = new FormattedText();

		SizeF? _measureSize;
		Brush? _brush;

		public Font? Font
		{
			get => _text.Font;
			set
			{
				_text.Font = value;
				_measureSize = null;
			}
		}
		
		protected IElement? Parent { get; set; }
		
		IElement? IElement.Parent
		{
			get => Parent;
			set => Parent = value;
		}

		public Brush? Brush
		{
			get => _brush;
			set => _brush = _text.ForegroundBrush = value;
		}

		public int Start { get; set; }
		public int Length => Text.Length;
		public int End => Start + Length;
		
		public int DocumentStart => Start + Parent?.DocumentStart ?? 0;

		public string Text
		{
			get => _text.Text;
			set
			{
				_text.Text = value;
				_measureSize = null;
			}
		}

		public SpanElement? Split(int index)
		{
			if (index >= Length)
				return null;
			var text = Text;
			Text = text.Substring(0, index);
			if (index >= text.Length)
				return null;
			var newSpan = new SpanElement { Text = text.Substring(index), Font = Font };
			return newSpan;
		}

		IElement? IElement.Split(int index) => Split(index);

		internal SpanElement WithText(string text)
		{
			if (text == Text)
				return this;
			var span = new SpanElement { Font = Font, Brush = Brush, Text = text };
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

        public int GetIndexAt(Chunk chunk, PointF point)
        {
			if (point.X > chunk.Bounds.Right || point.Y > chunk.Bounds.Bottom)
				return -1;
			if (point.X < chunk.Bounds.Left || point.Y < chunk.Bounds.Top)
				return -1;
			var spanX = chunk.Bounds.X;
			var spanLength = Length;
			for (int i = 0; i < spanLength; i++)
			{
				var spanSize = Font?.MeasureString(Text.Substring(i, 1)) ?? SizeF.Empty;
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
			if (Brush is SolidBrush brush && span.Brush is SolidBrush otherBrush)
			{
				if (brush.Color != otherBrush.Color)
					return false;
			}
			return Font == span.Font;
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

		public IEnumerable<IInlineElement> EnumerateInlines(int start, int end)
		{
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
			if (start == 0 && end == Length)
			{
				yield return this;
				yield break;
			}
			if (start == 0)
			{
				yield return new SpanElement { Start = start, Font = Font, Brush = Brush, Text = Text.Substring(0, end) };
				yield break;
			}
			if (end == Length)
			{
				yield return new SpanElement { Start = start, Font = Font, Brush = Brush, Text = Text.Substring(start) };
				yield break;
			}
			yield return new SpanElement { Start = start, Font = Font, Brush = Brush, Text = Text.Substring(start, end - start) };
		}

		public void Paint(Chunk chunk, Graphics graphics, RectangleF clipBounds)
		{
			graphics.DrawText(_text, chunk.Bounds.Location);
		}

		public SizeF Measure(SizeF availableSize, out float baseline)
		{
			if (_measureSize == null)
			{
				_measureSize = _text.Measure();
			}
			baseline = Font?.Baseline ?? 0;
			return _measureSize.Value;
		}

	}
}
