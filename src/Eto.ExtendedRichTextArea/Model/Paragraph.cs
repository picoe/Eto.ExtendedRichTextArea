using Eto.Drawing;
using Eto.ExtendedRichTextArea.Measure;

namespace Eto.ExtendedRichTextArea.Model
{
	public class Paragraph : Element<Run>
	{
		internal override Element<Run> Create() => new Paragraph();
		internal override Run CreateElement() => new Run();

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
			if (size.Height <= 0 && GetTopParent(this) is Document doc)
			{
				size.Height = doc.DefaultFont.LineHeight;
			}
			return size;
		}
		
		public override void OffsetElement(Measurement measurement)
		{
			// move next line below the current one
			measurement.CurrentLocation = measurement.CurrentLine?.Bounds.BottomLeft ?? measurement.CurrentParagraph?.Bounds.BottomLeft ?? PointF.Empty;
		}
		

		internal Paragraph? Split(int index) => (Paragraph?)((IElement)this).Split(index);
	}
}
