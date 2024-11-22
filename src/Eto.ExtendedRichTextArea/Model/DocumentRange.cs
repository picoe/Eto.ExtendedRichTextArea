using Eto.Drawing;

namespace Eto.ExtendedRichTextArea.Model
{
	public class DocumentRange
	{
		internal Document? Document { get; set; }
		public int Start { get; }
		public int Length { get; }
		public int End => Start + Length;

		List<RectangleF>? _bounds;

		public DocumentRange(int start, int end)
		{
			if (start <= end)
			{
				Start = start;
				Length = end - start;
			}
			else
			{
				Start = end;
				Length = start - end;
			}
		}
		
		public void CalculateBounds()
		{
			if (Document == null)
				return;
			
			_bounds ??= new List<RectangleF>();
			_bounds.Clear();
			RectangleF bounds = RectangleF.Empty;
			Span? lastSpan = null;
			// TODO: trim mid spans for start/end
			foreach (var span in Document.EnumerateSpans(Start, End))
			{
				var spanBounds = span.Bounds;
				if (bounds.IsEmpty)
				{
					bounds = span.Bounds;
					var documentIndex = span.DocumentIndex;
					if (documentIndex < Start)
					{
						var point = span.GetPointAtIndex(Start - documentIndex);
						if (point != null)
						{
							bounds.Width -= point.Value.X - bounds.X;
							bounds.X = point.Value.X;
						}
					}
				}
				else if (span.Bounds.Y != bounds.Y || span.Bounds.X < bounds.X)
				{
					_bounds.Add(bounds);
					bounds = span.Bounds;
				}
				else
				{
					// combine bounds
					bounds.Right = span.Bounds.Right;
					bounds.Height = Math.Max(bounds.Height, span.Bounds.Height);					
				}
				lastSpan = span;
			}
			if (!bounds.IsEmpty)
			{
				if (lastSpan != null)
				{
					var documentIndex = lastSpan.DocumentIndex;
					if (documentIndex + lastSpan.Length > End)
					{
						var point = lastSpan.GetPointAtIndex(End - documentIndex);
						bounds.Width = point?.X - bounds.X ?? bounds.Width;
					}
				}
				_bounds.Add(bounds);
			}
		}
		
		internal void Paint(Graphics graphics)
		{
			if (_bounds == null)
				return;
			foreach (var bounds in _bounds)
			{
				graphics.FillRectangle(SystemColors.Highlight, bounds);
			}
		}
		
	}
}
