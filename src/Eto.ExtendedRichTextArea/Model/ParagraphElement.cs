using Eto.Drawing;

namespace Eto.ExtendedRichTextArea.Model
{
	public class ParagraphElement : ContainerElement<IInlineElement>
	{
		List<Line>? _lines;

		public static readonly char SoftBreakCharacter = '\x2028'; // Unicode line separator

		protected override ContainerElement<IInlineElement> Create() => new ParagraphElement();
		protected override IInlineElement CreateElement() => new TextElement();

		public override PointF? GetPointAt(int start, out Line? line)
		{
			if (_lines == null)
			{
				line = null;
				return null;
			}
			start += DocumentStart;
			for (int i = 0; i < _lines.Count; i++)
			{
				Line? currentLine = _lines[i];
				if (currentLine.Start > start)
					break;
				if (currentLine.End < start)
					continue;
				var lineStart = start - currentLine.Start;

				for (int j = 0; j < currentLine.Count; j++)
				{
					Chunk? chunk = currentLine[j];
					if (chunk.Start <= lineStart && chunk.End >= lineStart)
					{
						line = currentLine;
						return chunk.GetPointAt(lineStart - chunk.Start);
					}
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
			SizeF totalSize = SizeF.Empty;
			int chunkStart = 0;
			var separatorLength = SeparatorLength;
			int lineStart = DocumentStart;
			PointF elementLocation = location;
			var line = new Line { Start = lineStart };
			for (int i = 0; i < Count; i++)
			{
				if (i > 0)
					lineStart += separatorLength;
				var element = this[i];
				// element.Start = start; // shouldn't be needed anymore, but maybe do this in release mode to self-heal?

				var elementStart = 0;
				do
				{
					var breakIndex = element.Text?.IndexOf(SoftBreakCharacter, elementStart) ?? 0; // line break
					var elementLength = (breakIndex >= 0 ? breakIndex : element.Length) - elementStart;

					var available = availableSize - new SizeF(location.X, 0);
					var elementSize = element.Measure(defaultAttributes, available, elementStart, elementLength, out var baseline);

					void NextLine()
					{
						line.End = lineStart;
						line.Bounds = new RectangleF(location, size);
						_lines.Add(line);
						totalSize.Height += size.Height;
						totalSize.Width = Math.Max(totalSize.Width, size.Width);

						if (elementStart > 0)
						{
							lineStart++; // soft break
						}

						line = new Line { Start = lineStart };
						location.Y += size.Height;
						elementLocation = location;
						size = SizeF.Empty;
						chunkStart = 0;
					}

					if (elementStart > 0)
					{
						NextLine();
						// line.Start++; // skip the line break character
						// elementStart++; // start at 1 to skip the line break character
					}

					if (elementLocation.X + elementSize.Width > availableSize.Width)
					{
						// TODO: this isn't quite done yet, but we need to split elements that are too wide for the line
						if (elementLocation.X <= availableSize.Width)
						{
							// split into possibly multiple lines here (if one element is very long)
							var chunk = new Chunk(element, chunkStart, elementLength, new RectangleF(elementLocation, elementSize), elementStart);
							line.Add(chunk);
							lineStart += elementLength;
						}

						// new line for the rest of it!
						NextLine();
					}
					else
					{
						var chunk = new Chunk(element, chunkStart, elementLength, new RectangleF(elementLocation, elementSize), elementStart);
						line.Add(chunk);
					}
					line.Baseline = Math.Max(line.Baseline, baseline);

					size.Height = Math.Max(size.Height, elementSize.Height);
					size.Width += elementSize.Width;
					elementLocation.X += elementSize.Width;

					lineStart += elementLength;
					chunkStart += elementLength;
					elementStart += elementLength + 1; // skip soft line break
				} while (elementStart <= element.Length);
			}
			line.End = lineStart;
			if (size.Height <= 0)
				size.Height = Attributes?.Merge(defaultAttributes, false).Font?.LineHeight ?? Document.GetDefaultFont().LineHeight;
			line.Bounds = new RectangleF(location, size);
			_lines.Add(line);

			totalSize.Height += size.Height;
			totalSize.Width = Math.Max(totalSize.Width, size.Width);
			
			return totalSize;
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
					return index + line.Start - DocumentStart;
			}
			return -1;
		}

		public override bool InsertAt(int start, IElement element)
		{
			if (element is TextElement insertSpan && Parent?.Separator != null)
			{
				var text = insertSpan.Text;
				var lines = text.Split(new[] { Parent.Separator }, StringSplitOptions.None);
				if (lines.Length == 0)
					return false;
				if (lines.Length > 1)
				{
					var insertParagraph = this;
					for (int i = 0; i < lines.Length; i++)
					{
						if (i > 0)
						{
							// next line splits or adds a new paragraph
							var right = ((IBlockElement)insertParagraph).Split((int)start) ?? Parent.CreateElement();
							if (right != null && right is ParagraphElement rightParagraph)
							{
								// Parent.Adjust(Parent.IndexOf(this), -right.Length);
								// if we split, we need to insert right paragraph after the current paragraph
								Parent.Insert(Parent.IndexOf(insertParagraph) + 1, right);
								insertParagraph = rightParagraph;
								start = 0;
							}
						}

						var lineText = lines[i];
						var span = insertSpan.WithText(lineText);
						if (!insertParagraph.InsertInParagraph(start, span))
							return false;

						start += span.Length;
					}
					return true;
				}
				else
				{
					insertSpan.Start = start;
				}
			}

			return InsertInParagraph(start, element);
		}

		private bool InsertInParagraph(int start, IElement element)
		{
			if (element is not IInlineElement inline)
				return false;

			var (child, index, position) = FindAt(start);

			inline.Start = start;
			if (index >= 0 && child != null)
			{
				if (child.Merge(start - child.Start, inline))
				{
					return true;
				}
				else
				{
					var rightSpan = child.Split(start - child.Start);
					if (rightSpan is IInlineElement rightElement)
					{
						Insert(index + 1, rightElement);
					}
				}
			}

			return base.InsertAt(start, element);
		}

		/*
				internal bool InsertInParagraph(int start, IInlineElement element)
				{
					var item = FindAt(start);
					if (item.child != null)
					{
						if (item.child.Merge(start - item.child.Start, element))
							AdjustLength(element.Length);
						else
						{
							var rightSpan = item.child.Split(start - item.child.Start);
							if (rightSpan is IInlineElement rightElement)
							{
								Adjust(item.index, -rightElement.Length);
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
				*/

		public override void Paint(Graphics graphics, RectangleF clipBounds)
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

		public override bool IsValid()
		{
			if (!base.IsValid())
				return false;
				
			/*
			if (_lines == null || _lines.Count == 0)
				return true;

			var idx = 0;
			for (int i = 0; i < _lines.Count; i++)
			{
				Line? line = _lines[i];
				if (line == null || line.Count == 0)
					continue;
				if (line.Start < 0 || line.End < line.Start || line.Bounds.IsEmpty)
					return false;
				for (int j = 0; j < line.Count; j++)
				{

				}
				if (line.Start != idx)
					return false;
				idx = line.End;
			}*/

			return true;
		}

	}
}
