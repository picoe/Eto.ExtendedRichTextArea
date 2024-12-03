using Eto.Drawing;

namespace Eto.ExtendedRichTextArea.Model
{
	public class ParagraphElement : ContainerElement<IInlineElement>
	{
		List<Line>? _lines;

		internal override ContainerElement<IInlineElement> Create() => new ParagraphElement();
		internal override IInlineElement CreateElement() => new SpanElement();

		public override PointF? GetPointAt(int start, out Line? line)
		{
			if (_lines == null)
			{
				line = null;
				return null;
			}
			for (int i = 0; i < _lines.Count; i++)
			{
				Line? currentLine = _lines[i];
				if (currentLine.Start > start)
					break;
				if (currentLine.End < start)
					continue;
				var lineStart = start - currentLine.Start;
				foreach (var chunk in currentLine)
				{
					if (chunk.Start <= lineStart && chunk.End >= lineStart)
					{
						line = currentLine;
						return chunk.GetPointAt(lineStart - chunk.Start);
					}
					// lineStart -= chunk.Length;
				}
			}
			line = null;
			return Bounds.Location;
		}

		protected override SizeF MeasureOverride(Attributes defaultAttributes, SizeF availableSize, PointF location)
		{
			_lines ??= new List<Line>();
			_lines.Clear();

			availableSize += (SizeF)location;
			SizeF size = SizeF.Empty;
			int start = 0;
			var docStart = DocumentStart;
			var separatorLength = Separator?.Length ?? 0;
			PointF elementLocation = location;
			var line = new Line { Start = start, DocumentStart = docStart };
			for (int i = 0; i < Count; i++)
			{
				if (i > 0)
					start += separatorLength;
				var element = this[i];
				element.Start = start;
				
				var available = availableSize - new SizeF(location.X, 0);
				var elementSize = element.Measure(defaultAttributes, available, out var baseline);

				if (elementLocation.X + elementSize.Width > availableSize.Width)
				{
					// wrap if needed!
					if (elementLocation.X <= availableSize.Width)
					{
						// split into possibly multiple lines here
						var chunk = new Chunk(element, start, start + element.Length, new RectangleF(elementLocation, elementSize));
						line.Add(chunk);
						start += element.Length;
					}

					line.End = start;
					line.Bounds = new RectangleF(location, size);
					_lines.Add(line);

					// new line for the rest of it!
					line = new Line { Start = start, DocumentStart = docStart + start };
					line.Baseline = Math.Max(line.Baseline, baseline);
					location.Y += size.Height;
					elementLocation = location;
					size = SizeF.Empty;
				}
				else
				{
					line.Baseline = Math.Max(line.Baseline, baseline);
					var chunk = new Chunk(element, start, start + element.Length, new RectangleF(elementLocation, elementSize));
					line.Add(chunk);
				}				
				
				size.Height = Math.Max(size.Height, elementSize.Height);
				size.Width += elementSize.Width;
				elementLocation.X += elementSize.Width;
				
				start += element.Length;
			}
			line.End = start;
			if (size.Height <= 0)
				size.Height = Attributes?.Merge(defaultAttributes, false).Font?.LineHeight ?? Document.GetDefaultFont().LineHeight;
			
			line.Bounds = new RectangleF(location, size);
			_lines.Add(line);
			Length = start;
			return size;
		}
		
		public override int GetIndexAt(PointF point)
		{
			if (_lines == null)
				return -1;
			for (int i = 0; i < _lines.Count; i++)
			{
				Line? line = _lines[i];
				if (line == null)
					continue;
				if (line.Bounds.Bottom < point.Y)
					continue;
				var index = line.GetIndexAt(point);
				if (index >= 0)
					return index + line.Start;
			}
			return -1;
		}
		
		internal bool InsertInParagraph(int start, IInlineElement element)
		{
			var inline = Find(start);
			if (inline != null)
			{
				if (inline.Merge(start - inline.Start, element))
					Length += element.Length;
				else
				{
					var rightSpan = inline.Split(start - inline.Start);
					if (rightSpan is IInlineElement rightElement)
					{
						InsertAt(start, rightElement);
					}
					InsertAt(start, element);
				}
				return true;
			}
			else if (Count == 0)
			{
				Add(element);
				return true;
			}
			return false;
		}

		internal void Paint(Graphics graphics, RectangleF clipBounds)
		{
			if (_lines == null)
				return;
			for (int i = 0; i < _lines.Count; i++)
			{
				Line? line = _lines[i];
				if (!line.Bounds.Intersects(clipBounds))
					continue;
				line.Paint(graphics, clipBounds);
			}
		}

		public override IEnumerable<Chunk> EnumerateChunks(int start, int end)
		{
			if (_lines == null)
				yield break;
				
			for (int i = 0; i < _lines.Count; i++)
			{
				var line = _lines[i];
				if (line.Start >= end)
					break;
				if (line.End <= start)
					continue;
				var chunkStart = start - line.Start;
				var chunkEnd = end - line.Start;
				for (int j = 0; j < line.Count; j++)
				{
					Chunk? chunk = line[j];
					if (chunk.Start >= chunkEnd)
						break;
					if (chunk.End <= chunkStart)	
						continue;
					yield return chunk;
				}
			}
		}

		public override IEnumerable<Line> EnumerateLines(int start, bool forward = true)
		{
			if (_lines == null)
				yield break;
			var lines = forward ? _lines : _lines.Reverse<Line>();
			foreach (var line in lines)
			{
				if (forward && line.End < start)
					continue;
				else if (!forward && line.Start > start)
					continue;
					
				yield return line;
			}
		}

		internal ParagraphElement? Split(int index) => (ParagraphElement?)((IElement)this).Split(index);
	}
}
