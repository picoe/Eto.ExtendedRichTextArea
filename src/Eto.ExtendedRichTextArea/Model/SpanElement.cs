
using Eto.Drawing;

namespace Eto.ExtendedRichTextArea.Model
{

	public interface IInlineElement : IElement
	{
		bool Matches(IInlineElement element);
		bool Merge(int index, IInlineElement element);
		void Paint(Chunk chunk, Graphics graphics, RectangleF clipBounds);
		PointF? GetPointAt(Chunk chunk, int start);
		int GetIndexAt(Chunk chunk, PointF point);
		SizeF Measure(Attributes defaultAttributes, SizeF availableSize, out float baseline);
	}
	
	public class SpanElement : IInlineElement
	{
		FormattedText? _formattedText;

		SizeF? _measureSize;
		string? _text;

		Attributes? _attributes;

		public Attributes? Attributes
		{
			get => _attributes;
			set
			{
				_attributes = value;
				_measureSize = null;
			}
		}
		
		protected IElement? Parent { get; set; }
		
		IElement? IElement.Parent
		{
			get => Parent;
			set => Parent = value;
		}

		public int Start { get; set; }
		public int Length => Text.Length;
		public int End => Start + Length;
		
		public int DocumentStart => Start + Parent?.DocumentStart ?? 0;

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
			if (index >= Length)
				return null;
			var text = Text;
			Text = text.Substring(0, index);
			if (index >= text.Length)
				return null;
			var newSpan = new SpanElement { Text = text.Substring(index), Attributes = Attributes?.Clone() };
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
		
		public Font Font => Attributes?.Font ?? this.GetDocument()?.DefaultAttributes.Font ?? SystemFonts.Default();
		public Brush ForegroundBrush => Attributes?.ForegroundBrush ?? this.GetDocument()?.DefaultAttributes.ForegroundBrush ?? new SolidBrush(SystemColors.ControlText);

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
			_formattedText ??= new FormattedText { Text = Text };
			defaultAttributes.Apply(_formattedText);
			Attributes?.Apply(_formattedText);
			
			if (_measureSize == null)
			{
				_measureSize = _formattedText.Measure();
			}
			baseline = Font?.Baseline ?? 0;
			return _measureSize.Value;
		}

	}
}
