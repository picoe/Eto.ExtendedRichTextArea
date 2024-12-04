using Eto.Drawing;

namespace Eto.ExtendedRichTextArea.Model
{
	/// <summary>
	/// A chunk is a piece of an IInlineElement (usually SpanElement) that is broken up into smaller pieces 
	/// to fit within a line for wrapping.
	/// Not all inline elements can be broken up. For instance, an ImageElement cannot be broken up currently
	/// so it will only have a single chunk.
	/// </summary>
	public class Chunk
	{
		public IInlineElement Element { get; }
		public RectangleF Bounds { get; }
		public int Start { get; }
		public int End { get; }

		public int Length => End - Start;
		
		public int InlineStart { get; }
		public int InlineEnd => InlineStart + Length;

		public Chunk(IInlineElement element, int start, int end, RectangleF bounds, int inlineIndex = 0)
		{
			Element = element;
			Start = start;
			End = end;
			Bounds = bounds;
			InlineStart = inlineIndex;
		}

		internal void Paint(Line line, Graphics graphics, RectangleF clipBounds)
		{
			// graphics.DrawRectangle(Colors.Gray, Bounds);
			Element.Paint(line, this, graphics, clipBounds);
		}

		internal PointF? GetPointAt(int start)
		{
			return Element.GetPointAt(this, start);
		}
		
        public int GetIndexAt(PointF point)
        {
			return Element.GetIndexAt(this, point);
        }
	}
}
