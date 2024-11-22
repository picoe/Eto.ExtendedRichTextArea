using Eto.Drawing;

namespace Eto.ExtendedRichTextArea.Model
{
	public class Run : DocumentElement<Span>
	{
        protected override void OffsetElement(ref PointF location, ref SizeF size, SizeF elementSize)
        {
			// spans are stacked horizontally
			size.Height = Math.Max(size.Height, elementSize.Height);
			size.Width += elementSize.Width;
			location.X += elementSize.Width;
        }

		internal override DocumentElement<Span> Create() => new Run();

		internal Run? Split(int index) => (Run?)((IDocumentElement)this).Split(index);
	}
}
