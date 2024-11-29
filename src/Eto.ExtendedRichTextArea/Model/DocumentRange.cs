using Eto.Drawing;

namespace Eto.ExtendedRichTextArea.Model
{
	public class DocumentRange
	{
		Document? _document;
		internal Document? Document
		{
			get => _document;
			set
			{
				_document = value;
				_bounds = null;
			}
		}
		
		public int Start { get; }
		public int Length => End - Start;
		public int End { get; }
		public string Text => Document?.GetText(Start, Length) ?? string.Empty;

		public int OriginalStart { get; }

		List<RectangleF>? _bounds;

		public DocumentRange(int start, int end, int? originalStart = null)
		{
			OriginalStart = originalStart ?? start;
			Start = Math.Min(start, end);
			End = Math.Max(start, end);
		}
		
		public void CalculateBounds()
		{
			if (Document == null)
				return;
			
			_bounds ??= new List<RectangleF>();
			_bounds.Clear();
			RectangleF bounds = RectangleF.Empty;
			Chunk? lastChunk = null;
			// TODO: trim mid spans for start/end
			foreach (var chunk in Document.EnumerateChunks(Start, End))
			{
				var spanBounds = chunk.Bounds;
				if (bounds.IsEmpty)
				{
					bounds = chunk.Bounds;
					var documentIndex = chunk.Element.DocumentStart;
					if (documentIndex < Start)
					{
						var point = chunk.GetPointAt(Start - documentIndex);
						if (point != null)
						{
							bounds.Width -= point.Value.X - bounds.X;
							bounds.X = point.Value.X;
						}
					}
				}
				else if (chunk.Bounds.Y != bounds.Y || chunk.Bounds.X < bounds.X)
				{
					_bounds.Add(bounds);
					bounds = chunk.Bounds;
				}
				else
				{
					// combine bounds
					bounds.Right = chunk.Bounds.Right;
					bounds.Height = Math.Max(bounds.Height, chunk.Bounds.Height);					
				}
				lastChunk = chunk;
			}
			if (!bounds.IsEmpty)
			{
				if (lastChunk != null)
				{
					var documentIndex = lastChunk.Element.DocumentStart;
					if (documentIndex + lastChunk.Length > End)
					{
						var point = lastChunk.GetPointAt(End - documentIndex);
						bounds.Width = point?.X - bounds.X ?? bounds.Width;
					}
				}
				_bounds.Add(bounds);
			}
		}
		
		internal void Paint(Graphics graphics)
		{
			if (_bounds == null)
				CalculateBounds();
			if (_bounds == null)
				return;
			foreach (var bounds in _bounds)
			{
				graphics.FillRectangle(SystemColors.Highlight, bounds);
			}
		}

		public void SetAttributes(Attributes? selectionAttributes)
		{
			Document?.SetAttributes(Start, End, selectionAttributes);
		}
	}
}
