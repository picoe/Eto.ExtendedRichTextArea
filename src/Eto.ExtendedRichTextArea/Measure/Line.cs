using Eto.Drawing;
using Eto.ExtendedRichTextArea.Model;

using System.Collections.ObjectModel;

namespace Eto.ExtendedRichTextArea.Measure
{
	public class Line : Collection<Chunk>
	{
		public Paragraph Paragraph { get; }
		public Run Run { get; }
		public RectangleF Bounds { get; set; }

		public float Baseline { get; set; }

		public Line(Paragraph paragraph, Run run)
		{
			Paragraph = paragraph;
			Run = run;
		}

		public void Paint(Graphics graphics, RectangleF clipBounds)
		{
			for (int i = 0; i < Count; i++)
			{
				Chunk? chunk = this[i];
				if (chunk == null)
					continue;
				if (!chunk.Bounds.Intersects(clipBounds))
					continue;
				chunk.Paint(graphics, clipBounds);
			}
		}

		protected override void InsertItem(int index, Chunk item)
		{
			base.InsertItem(index, item);
			Bounds = RectangleF.Union(Bounds, item.Bounds);
		}

		protected override void RemoveItem(int index)
		{
			base.RemoveItem(index);
			Bounds = Items.Aggregate(RectangleF.Empty, (r, c) => RectangleF.Union(r, c.Bounds));
		}
		protected override void SetItem(int index, Chunk item)
		{
			base.SetItem(index, item);
			Bounds = Items.Aggregate(RectangleF.Empty, (r, c) => RectangleF.Union(r, c.Bounds));
		}

		protected override void ClearItems()
		{
			base.ClearItems();
			Bounds = new RectangleF(Bounds.Location, SizeF.Empty);
		}
	}
}
