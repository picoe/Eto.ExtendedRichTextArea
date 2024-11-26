using Eto.Drawing;

namespace Eto.ExtendedRichTextArea.Model
{

	public class RunElement : ContainerElement<IInlineElement>
	{
		List<Line>? _lines;

		// protected override string? Separator => "\x2028"; // line separator
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
				
				// wrap if needed!
				var available = availableSize - new SizeF(location.X, 0);
				var elementSize = element.Measure(defaultAttributes, available, out var baseline);

				if (elementLocation.X + elementSize.Width > availableSize.Width)
				{
					if (elementLocation.X <= availableSize.Width)
					{
						// split into possibly multiple lines here
						var chunk = new Chunk(element, start, start + element.Length, new RectangleF(elementLocation, elementSize));
						line.Add(chunk);
					}

					line.End = start;
					line.Bounds = new RectangleF(location, size);
					_lines.Add(line);
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
			line.Bounds = new RectangleF(location, size);
			_lines.Add(line);
			Length = start;
			return size;
		}

		internal override ContainerElement<IInlineElement> Create() => new RunElement();

		internal override IInlineElement CreateElement() => new SpanElement();

		internal RunElement? Split(int index) => (RunElement?)((IElement)this).Split(index);
		
		public override PointF? GetPointAt(int start)
		{
			if (_lines == null)
				return null;
			for (int i = 0; i < _lines.Count; i++)
			{
				Line? line = _lines[i];
				if (line.Start > start)
					break;
				if (line.End < start)
					continue;
				var lineStart = start - line.Start;
				foreach (var chunk in line)
				{
					if (chunk.Start <= lineStart && chunk.End >= lineStart)
						return chunk.GetPointAt(lineStart - chunk.Start);
					// lineStart -= chunk.Length;
				}
			}
			return Bounds.Location;
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
				var index = line.GetIndexAt(point);
				if (index >= 0)
					return index + line.Start;
			}
			return -1;
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

	}
}
