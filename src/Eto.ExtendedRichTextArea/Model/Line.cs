using System.Collections.ObjectModel;

using Eto.Drawing;

namespace Eto.ExtendedRichTextArea.Model
{
	/// <summary>
	/// A line is a single line of elements that can be rendered on the screen and is computed based on each Run.
	/// Unline a Run, which can span multiple lines when wrapping is enabled.
	/// </summary>
	public class Line : Collection<Chunk>
	{
		public int Start { get; set; }
		public int End { get; set; }
		public int Length => End - Start;
		public RectangleF Bounds { get; set; }		
		public float Baseline { get; set; }
		
		internal void Paint(Graphics graphics, RectangleF clipBounds)
		{
			for (int i = 0; i < Count; i++)
			{
				Chunk? chunk = this[i];
				if (!chunk.Bounds.Intersects(clipBounds))
					continue;
				chunk.Paint(this, graphics, clipBounds);
			}
		}

		internal int GetIndexAt(PointF point)
		{
			if (point.Y > Bounds.Bottom)
				return -1;
			if (point.Y < Bounds.Top)
				return -1;
			if (point.X > Bounds.Right)
				return Length;
			if (point.X < Bounds.Left)
				return 0;

			if (!Bounds.Contains(point))
				return -1;
								
			var linePoint = point; // - line.Bounds.Location;
			foreach (var chunk in this)
			{
				var index = chunk.GetIndexAt(linePoint);
				if (index >= 0)
					return index + chunk.Start;
			}
			return -1;
		}
	}
}
