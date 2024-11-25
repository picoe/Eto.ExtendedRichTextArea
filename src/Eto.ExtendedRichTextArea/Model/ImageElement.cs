
using Eto.Drawing;
using Eto.ExtendedRichTextArea.Measure;

namespace Eto.ExtendedRichTextArea.Model
{
	public class ImageElement : IInlineElement
	{
		public SizeF Size { get; set; }
		public int Start { get; set; }
		public int Length => 1;
		public int End => Start + Length;
		public RectangleF Bounds { get; internal set; }
		
		public Image? Image { get; set; }
		
		public int DocumentIndex => Start + Parent?.DocumentIndex ?? 0;

		public IElement? Parent { get; private set; }
		
		IElement? IElement.Parent
		{
			get => Parent;
			set => Parent = value;
		}
		string IElement.Text { get => string.Empty; set { } }

		public SizeF Measure(SizeF availableSize, PointF location)
		{
			Size = Image?.Size ?? SizeF.Empty;
			Bounds = new RectangleF(location, Size);
			return Size;
		}

		public void Paint(Graphics graphics, RectangleF clipBounds)
		{
			graphics.DrawImage(Image, Bounds);
		}

		public int RemoveAt(int index, int length)
		{
			return Length;
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
			return 0;
		}

		public PointF? GetPointAtIndex(int index)
		{
			if (index != 0)
				return null;
			return new PointF(Bounds.X, Bounds.Y);
		}

		public IEnumerable<(string text, int index)> EnumerateWords(int start, bool forward)
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

		public void Paint(Chunk chunk, Graphics graphics, RectangleF clipBounds)
		{
			graphics.DrawImage(Image, chunk.Bounds);
		}

		public IEnumerable<IInlineElement> EnumerateInlines(int start, int end)
		{
			yield return this;
		}

		public void Measure(Measurement measurement)
		{
			
		}

		public PointF? GetPointAt(Chunk chunk, int start)
		{
			throw new NotImplementedException();
		}
	}
}
