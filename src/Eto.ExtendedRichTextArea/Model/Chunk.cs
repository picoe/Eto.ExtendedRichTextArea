using Eto.Drawing;

namespace Eto.ExtendedRichTextArea.Model
{
	public class Chunk
	{
		public IInlineElement Element { get; }
		public RectangleF Bounds { get; }
		public int Start { get; }
		public int End { get; }

		public int Length => End - Start;
		
		public int InlineIndex { get; }

		public Chunk(IInlineElement element, int start, int end, RectangleF bounds, int inlineIndex = 0)
		{
			Element = element;
			Start = start;
			End = end;
			Bounds = bounds;
			InlineIndex = inlineIndex;
		}

		internal void Paint(Graphics graphics, RectangleF clipBounds)
		{
			// graphics.DrawRectangle(Colors.Gray, Bounds);
			Element.Paint(this, graphics, clipBounds);
		}

		internal PointF? GetPointAt(int start)
		{
			return Element.GetPointAt(this, start);
		}
		
        public int GetIndexAt(PointF point)
        {
			return Element.GetIndexAt(this, point) + Start;
        }
	}
}
