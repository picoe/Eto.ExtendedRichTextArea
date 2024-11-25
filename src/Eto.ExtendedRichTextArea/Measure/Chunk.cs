
using Eto.Drawing;
using Eto.ExtendedRichTextArea.Model;

namespace Eto.ExtendedRichTextArea.Measure
{
	public class Chunk
	{
		public IInlineElement Element { get; }
		public RectangleF Bounds { get; }
		public int Start { get; }
		public int End { get; }

		public int Length => End - Start;

		public Chunk(IInlineElement element, int start, int end, RectangleF bounds)
		{
			Element = element;
			Start = start;
			End = end;
			Bounds = bounds;
		}

		internal void Paint(Graphics graphics, RectangleF clipBounds)
		{
			Element.Paint(this, graphics, clipBounds);
		}

		internal PointF? GetPointAt(int start)
		{
			return Element.GetPointAt(this, start);
		}
	}
}
