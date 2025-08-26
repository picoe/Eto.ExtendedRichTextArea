using Eto.Drawing;

namespace Eto.ExtendedRichTextArea.Model
{
	public abstract class BlockContainerElement<T> : ContainerElement<T>
		where T : class, IBlockElement
	{
		public override int GetIndexAt(PointF point)
		{
			if (point.Y < Bounds.Top)
				return 0;
			if (point.Y > Bounds.Bottom)
				return Length;
			for (int i = 0; i < Count; i++)
			{
				var element = this[i];

				// too far, break!
				if (point.Y < element.Bounds.Top)
					break;
				if (point.Y >= element.Bounds.Bottom)
					continue;

				// traverse containers
				var index = element.GetIndexAt(point);
				if (index >= 0)
					return index + element.Start;
			}
			return Length;
		}

		public override PointF? GetPointAt(int start, out Line? line)
		{
			var separatorLength = SeparatorLength;
			for (int i = 0; i < Count; i++)
			{
				var child = this[i];
				if (start > child.Length)
				{
					start -= child.Length + separatorLength;
					continue;
				}
				var point = child.GetPointAt(start, out line);
				if (point.HasValue)
				{
					point = new PointF(point.Value.X, point.Value.Y);
					return point;
				}
			}
			line = null;
			return null;

		}

		public override IEnumerable<Chunk> EnumerateChunks(int start, int end)
		{
			for (int i = 0; i < Count; i++)
			{
				var element = this[i];
				if (element.Start >= end)
					break;
				if (element.End <= start)
					continue;
				var containerStart = Math.Max(start - element.Start, 0);
				var containerEnd = Math.Min(end - element.Start, element.Length);
				foreach (var inline in element.EnumerateChunks(containerStart, containerEnd))
				{
					yield return inline;
				}
			}
		}

		public override IEnumerable<Line> EnumerateLines(int start, bool forward = true)
		{
			var collection = forward ? this : this.Reverse();
			var elementStart = start - DocumentStart;
			foreach (var element in collection)
			{
				if (forward && element.End < elementStart)
					continue;
				else if (!forward && element.Start > elementStart)
					continue;
				foreach (var line in element.EnumerateLines(start, forward))
				{
					yield return line;
				}
			}
			// empty container
			if (Count == 0)
				yield return new Line { Start = DocumentStart, Bounds = Bounds };

		}

		public override void Paint(Graphics graphics, RectangleF clipBounds)
		{
			for (int i = 0; i < Count; i++)
			{
				var element = this[i];
				element.Paint(graphics, clipBounds);
			}
		}
	}
}
