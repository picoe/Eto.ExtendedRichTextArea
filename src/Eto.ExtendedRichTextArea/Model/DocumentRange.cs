using Eto.Drawing;

namespace Eto.ExtendedRichTextArea.Model
{
	public class DocumentRange
	{
		internal Document? Document { get; set; }
		public int Start { get; }
		public int Length => Math.Abs(End - Start);
		public int End { get; }
		
		public string Text => Document?.GetText(Start, Length) ?? string.Empty;

		List<RectangleF>? _bounds;

		public DocumentRange(int start, int end)
		{
			Start = start;
			End = end;
		}
		
		public void CalculateBounds()
		{
			if (Document == null)
				return;
			int start, end;
			if (Start < End)
			{
				start = Start;
				end = End;
			}
			else
			{
				start = End;
				end = Start;
			}
			
			_bounds ??= new List<RectangleF>();
			_bounds.Clear();
			RectangleF bounds = RectangleF.Empty;
			IInlineElement? lastSpan = null;
			// TODO: trim mid spans for start/end
			foreach (var span in Document.EnumerateInlines(start, end))
			{
				var spanBounds = span.Bounds;
				if (bounds.IsEmpty)
				{
					bounds = span.Bounds;
					var documentIndex = span.DocumentIndex;
					if (documentIndex < start)
					{
						var point = span.GetPointAtIndex(start - documentIndex);
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
					if (documentIndex + lastSpan.Length > end)
					{
						var point = lastSpan.GetPointAtIndex(end - documentIndex);
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
