
using Eto.Drawing;

namespace Eto.ExtendedRichTextArea.Model
{
	public class ImageElement : IInlineElement
	{
		public SizeF Size { get; set; }
		public int Start { get; set; }
		public int Length => 1;
		public int End => Start + Length;
		
		public Image? Image { get; set; }
		
		int? _documentStart;
		public int DocumentStart => _documentStart ??= Start + Parent?.DocumentStart ?? 0;

		public IElement? Parent { get; private set; }
		
		public Attributes? Attributes { get; set; }
		
		IElement? IElement.Parent
		{
			get => Parent;
			set => Parent = value;
		}
		string IElement.Text { get => string.Empty; set { } }

		public int RemoveAt(int index, int length)
		{
			return Length;
		}

		public IEnumerable<(string text, int start)> EnumerateWords(int start, bool forward)
		{
			yield break;
		}

		public IElement? Split(int index) => null;

		public void MeasureIfNeeded() => Parent?.MeasureIfNeeded();

		public bool Matches(IInlineElement element) => false;

		public bool Merge(int index, IInlineElement element)
		{
			return false;
		}

		public IEnumerable<IInlineElement> EnumerateInlines(int start, int end, bool trim)
		{
			yield return this;
		}

		public void Paint(Chunk chunk, Graphics graphics, RectangleF clipBounds)
		{
			graphics.DrawImage(Image, chunk.Bounds);
		}

		public PointF? GetPointAt(Chunk chunk, int start)
		{
			if (start == 1)
				return chunk.Bounds.TopRight;
			return chunk.Bounds.Location;
		}

		public int GetIndexAt(Chunk chunk, PointF point)
		{
			if (point.X > chunk.Bounds.Right || point.Y > chunk.Bounds.Bottom)
				return -1;
			if (point.X < chunk.Bounds.Left || point.Y < chunk.Bounds.Top)
				return -1;
			if (point.X > chunk.Bounds.Left + chunk.Bounds.Width / 3)
				return 1;
			return 0;
		}

		public SizeF Measure(Attributes defaultAttributes, SizeF availableSize, out float baseline)
		{
			_documentStart = null;
			Size = Image?.Size ?? SizeF.Empty;
			baseline = Size.Height;
			return Size;
		}

		public IEnumerable<IElement> Enumerate(int start, int end, bool trimInlines)
		{
			yield return this;
		}
	}
}
