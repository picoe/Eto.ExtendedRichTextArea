
using Eto.Drawing;

namespace Eto.ExtendedRichTextArea.Model
{
	public interface IInlineElement : IElement
	{
		bool Matches(IInlineElement element);
		bool Merge(int index, IInlineElement element);
		void Paint(Line line, Chunk chunk, Graphics graphics, RectangleF clipBounds);
		PointF? GetPointAt(Chunk chunk, int start);
		int GetIndexAt(Chunk chunk, PointF point);
		SizeF Measure(Attributes defaultAttributes, SizeF availableSize, int start, int length, out float baseline);
	}
}
