using System.Collections;

using Eto.Drawing;

namespace Eto.ExtendedRichTextArea.Model
{
	public interface IBlockElement : IElement, IList
	{
		string? Separator { get; }
		int SeparatorLength { get; }
		RectangleF Bounds { get; }
		IElement CreateElement();
		SizeF Measure(Attributes defaultAttributes, SizeF availableSize, PointF location);
		int GetIndexAt(PointF point);
		PointF? GetPointAt(int start, out Line? line);
		IEnumerable<Chunk> EnumerateChunks(int start, int end);
		IEnumerable<Line> EnumerateLines(int start, bool forward = true);
		void Paint(Graphics graphics, RectangleF clipBounds);
		Attributes GetAttributes(Attributes defaultAttributes, int start, int end);
		void SetAttributes(int start, int end, Attributes? attributes);
		void Adjust(int startIndex, int length);
		void AdjustLength(int length);
		bool IsValid();
	}
}
