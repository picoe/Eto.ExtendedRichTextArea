using Eto.Drawing;

namespace Eto.ExtendedRichTextArea.Model
{
	public class Paragraph : DocumentElement<Run>
	{
		internal override DocumentElement<Run> Create() => new Paragraph();

        protected override void OffsetElement(ref PointF location, ref SizeF size, SizeF elementSize)
        {
			// runs are stacked vertically with no space inbetween
			size.Width = Math.Max(size.Width, elementSize.Width);
			size.Height += elementSize.Height;
			location.Y += elementSize.Height;
        }

		protected override SizeF MeasureOverride(SizeF availableSize, PointF location)
		{
			var size = base.MeasureOverride(availableSize, location);
			if (size.Height <= 0 && TopParent is Document doc)
			{
				size.Height = doc.DefaultFont.LineHeight;
			}
			return size;
		}

		internal Paragraph? Split(int index) => (Paragraph?)((IDocumentElement)this).Split(index);
	}
}
