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
			var separatorLength = SeparatorLength;
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

		public override bool InsertAt(int start, IElement element)
		{
			if (element is SpanElement insertSpan && Parent?.Separator != null)
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

		// public override void SetAttributes(int start, int end, Attributes? attributes)
		// {
		// 	for (int j = 0; j < Count; j++)
		// 	{
		// 		var inline = this[j];
		// 		if (inline == null)
		// 			continue;
		// 		if (end <= inline.Start)
		// 			break;
		// 		if (start >= inline.End)
		// 			continue;

		// 		IElement applySpan = inline;
		// 		var elementStart = inline.Start;
		// 		if (start > elementStart && start < elementStart + inline.Length)
		// 		{
		// 			// need to split and apply attributes to right side only
		// 			var right = inline.Split(start - elementStart);
		// 			if (right != null && inline.Parent is IBlockElement container)
		// 			{
		// 				container.InsertAt(inline.End, right);

		// 				applySpan = right; // apply new attributes to the right side
		// 				elementStart = right.Start;
		// 				if (end > elementStart && end < elementStart + applySpan.Length)
		// 				{
		// 					// need to split again as the end is in the middle of the right side
		// 					right = applySpan.Split(end - elementStart);
		// 					if (right != null && applySpan.Parent is IBlockElement container2)
		// 					{
		// 						container2.InsertAt(applySpan.End, right);
		// 					}
		// 				}
		// 			}
		// 		}
		// 		if (end > elementStart && end < elementStart + applySpan.Length)
		// 		{
		// 			// need to split and apply attributes to left side
		// 			var right = applySpan.Split(end - elementStart);
		// 			if (right != null && applySpan.Parent is IBlockElement container)
		// 			{
		// 				container.InsertAt(applySpan.End, right);
		// 			}
		// 		}
		// 		applySpan.Attributes = UpdateAttributes(attributes, applySpan.Attributes);
		// 	}
		// }
		
	}
}
