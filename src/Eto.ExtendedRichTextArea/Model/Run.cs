using System.Security.Cryptography.X509Certificates;

using Eto.Drawing;
using Eto.ExtendedRichTextArea.Measure;

namespace Eto.ExtendedRichTextArea.Model
{
	public class Run : Element<IInlineElement>
	{
        protected override void OffsetElement(ref PointF location, ref SizeF size, SizeF elementSize)
        {
			// spans are stacked horizontally
			size.Height = Math.Max(size.Height, elementSize.Height);
			size.Width += elementSize.Width;
			location.X += elementSize.Width;
        }


		internal override Element<IInlineElement> Create() => new Run();

		internal override IInlineElement CreateElement() => new Span();

		internal Run? Split(int index) => (Run?)((IElement)this).Split(index);
	}
}
