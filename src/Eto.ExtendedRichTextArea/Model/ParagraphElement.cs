using Eto.Drawing;

namespace Eto.ExtendedRichTextArea.Model
{
	public class ParagraphElement : ContainerElement<RunElement>
	{
		internal override ContainerElement<RunElement> Create() => new ParagraphElement();
		internal override RunElement CreateElement() => new RunElement();

		public override PointF? GetPointAt(int index)
		{
			var element = Find(index);
			var point = element?.GetPointAt(index - element.Start);
			return point ?? Bounds.Location;
		}

		protected override SizeF MeasureOverride(SizeF availableSize, PointF location)
		{
			SizeF size = SizeF.Empty;
			int index = 0;
			var separatorLength = Separator?.Length ?? 0;
			PointF elementLocation = location;
			for (int i = 0; i < Count; i++)
			{
				if (i > 0)
					index += separatorLength;
				var element = this[i];
				element.Start = index;
				var elementSize = element.Measure(availableSize, elementLocation);
				
				size.Width = Math.Max(size.Width, elementSize.Width);
				size.Height += elementSize.Height;
				location.Y += elementSize.Height;
				index += element.Length;
			}
			Length = index;

			if (size.Height <= 0 && GetTopParent(this) is Document doc)
			{
				size.Height = doc.DefaultFont.LineHeight;
			}
			return size;
		}
		
		internal bool InsertInParagraph(int start, IInlineElement element)
		{
			var run = Find(start);
			var span = run?.Find(start - run.Start);
			if (span != null && run != null)
			{
				if (!span.Merge(start - run.Start - span.Start, element))
				{
					var rightSpan = span.Split(start - run.Start - span.Start);
					if (rightSpan is IInlineElement rightElement)
					{
						run.InsertAt(start - run.Start, rightElement);
					}
					run.InsertAt(start - run.Start, element);
				}
				else
					run.Recalculate(run.Start);
				Adjust(IndexOf(run), element.Length);
				return true;
			}
			else if (run != null)
			{
				var rightRun = run.Split(start - run.Start);
				if (rightRun != null)
				{
					// shouldn't happen?!
					InsertAt(start - Start, rightRun);
				}
				run.Add(element);
				Adjust(IndexOf(run), element.Length);
				return true;
			}
			else if (Count == 0)
			{
				Add(new RunElement { element });
				return true;
			}
			return false;
		}


		internal ParagraphElement? Split(int index) => (ParagraphElement?)((IElement)this).Split(index);
	}
}
