
using Eto.Drawing;

namespace Eto.ExtendedRichTextArea.Model
{
	public class Span : IDocumentElement
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
		
		protected IDocumentElement? Parent { get; set; }
		
		IDocumentElement? IDocumentElement.Parent
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
		public RectangleF Bounds { get; internal set; }
		
		public int DocumentIndex => Start + Parent?.DocumentIndex ?? 0;

		public string Text
		{
			get => _text.Text;
			set
			{
				_text.Text = value;
				_measureSize = null;
			}
		}

		public Span? Split(int index)
		{
			if (index >= Length)
				return null;
			var text = Text;
			Text = text.Substring(0, index);
			if (index >= text.Length)
				return null;
			var newSpan = new Span { Text = text.Substring(index), Font = Font };
			return newSpan;
		}

		IDocumentElement? IDocumentElement.Split(int index) => Split(index);

		public SizeF Measure(SizeF availableSize, PointF location)
		{
			if (_measureSize == null)
			{
				_text.MaximumWidth = availableSize.Width;
				_measureSize = _text.Measure();
			}
			Bounds = new RectangleF(location, _measureSize.Value);
			return _measureSize.Value;
		}

		internal void Paint(Graphics graphics, RectangleF clipBounds)
		{
			graphics.DrawText(_text, Bounds.Location);
		}

		internal bool Matches(Span insertSpan)
		{
			return Font == insertSpan.Font && Brush == insertSpan.Brush;
		}

		internal Span WithText(string text)
		{
			if (text == Text)
				return this;
			var span = new Span { Font = Font, Brush = Brush, Text = text };
			return span;
		}

		public int Remove(int index, int length)
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

        public int GetIndexAtPoint(PointF point)
        {
			if (point.X > Bounds.Right || point.Y > Bounds.Bottom)
				return -1;
			if (point.X < Bounds.Left || point.Y < Bounds.Top)
				return -1;
			var spanX = Bounds.X;
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

		public PointF? GetPointAtIndex(int index)
		{
			if (index < 0 || index > Length)
				return null;
			if (index == Length)
				return new PointF(Bounds.Right, Bounds.Y);
			if (index == 0)
				return new PointF(Bounds.X, Bounds.Y);
			var len = index;
			var text = Text.Substring(0, len);
			var size = Font?.MeasureString(text) ?? SizeF.Empty;
			return new PointF(Bounds.X + size.Width, Bounds.Y);

		}

		public IEnumerable<(string text, int index)> EnumerateWords(int start, bool forward)
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
	}
}
