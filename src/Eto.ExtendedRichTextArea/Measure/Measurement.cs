using Eto.Drawing;
using Eto.ExtendedRichTextArea.Model;

using System.Collections.ObjectModel;

namespace Eto.ExtendedRichTextArea.Measure
{
	public class Measurement : Collection<Line>
	{
		public Measurement(Document document, SizeF availableSize)
		{
			Document = document;
			AvailableSize = availableSize;
		}
		public RectangleF Bounds { get; set; }
		public SizeF AvailableSize { get; }

		public Document Document { get; }

		public PointF CurrentLocation { get; set; }

		internal Paragraph? CurrentParagraph { get; set; }
		internal Run? CurrentRun { get; set; }
		internal Line? CurrentLine { get; set; }

		public void AddNewLine(Run? run = null)
		{
			run ??= CurrentRun;
			if (run == null)
				throw new InvalidOperationException("Run must be specified if CurrentRun is null");

			if (run.Parent is not Paragraph paragraph)
				throw new InvalidOperationException("Run must be in a paragraph");
			var oldLine = CurrentLine;
			var newLine = new Line(paragraph, run)
			{
				// allow for spacing between lines?
				Bounds = new RectangleF(oldLine?.Bounds.BottomLeft ?? PointF.Empty, SizeF.Empty)
			};
			CurrentLine = newLine;
			CurrentParagraph = paragraph;
			CurrentRun = run;
			Add(newLine);
		}

		public void Measure()
		{
			Document.Measure(this);
			ExpandBounds();
		}

		private void ExpandBounds()
		{
			if (CurrentLine != null)
			{
				Bounds = RectangleF.Union(Bounds, CurrentLine.Bounds);
			}
		}

		public void Paint(Graphics graphics, RectangleF clipBounds)
		{
			for (int i = 0; i < Count; i++)
			{
				Line? line = this[i];
				if (line == null)
					continue;
				if (!line.Bounds.Intersects(clipBounds))
					continue;
				line.Paint(graphics, clipBounds);
			}
		}

		internal void AddChunk(IInlineElement inline, int start, int end, SizeF size)
		{
			if (CurrentLine == null)
				AddNewLine();
			if (CurrentLine == null)
				throw new InvalidOperationException("CurrentLine should not be null");
			var bounds = new RectangleF(CurrentLocation, size);
			CurrentLine.Add(new Chunk(inline, start, end, bounds));
			// offset between chunks?
			CurrentLocation = new PointF(CurrentLocation.X + size.Width, CurrentLocation.Y);
		}
		
		public PointF? GetPointAt(int start)
		{
			foreach (var line in this)
			{
				if (line.Run.Start > start)
					break;
				if (line.Run.End < start)
					continue;
				var lineStart = start - line.Paragraph.Start - line.Run.Start;
				foreach (var chunk in line)
				{
					if (chunk.Start <= lineStart && chunk.End >= lineStart)
						return chunk.GetPointAt(lineStart);
					lineStart -= chunk.Length;
				}
			}
			return null;
		}
		
	}
}
