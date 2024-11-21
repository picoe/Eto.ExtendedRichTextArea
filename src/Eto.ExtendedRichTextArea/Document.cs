using System;
using Eto.Forms;
using Eto.Drawing;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace Eto.ExtendedRichTextArea
{
	public enum DocumentNavigationMode
	{
		PreviousLine,
		NextLine,
		EndOfLine,
		BeginningOfLine

	}

	public interface IDocumentElement
	{
		int Start { get; set; }
		int Length { get; }
		int End { get; }
		RectangleF Bounds { get; }
		string Text { get; internal set; }
		IDocumentElement Parent { get; set; }
		IDocumentElement Split(int index);
		SizeF Measure(SizeF availableSize, PointF location);
		void Remove(int index, int length);
		void Recalculate(int index);
		int GetIndexAtPoint(PointF point);
		PointF? GetPointAtIndex(int index);		
	}

	public abstract class DocumentElement<T> : IEnumerable<T>, IDocumentElement
		where T : class, IDocumentElement, new()
	{
		readonly List<T> _elements = new List<T>();
		
		public int Start { get; private set; }
		public int Length { get; private set; }
		public int End => Start + Length;
		public RectangleF Bounds { get; private set; }
		
		protected IDocumentElement Parent { get; private set; }
		
		protected IDocumentElement TopParent
		{
			get
			{
				var parent = Parent;
				while (parent?.Parent != null)
				{
					parent = parent.Parent;
				}
				return parent;
			}
		}
		
		IDocumentElement IDocumentElement.Parent
		{
			get => Parent;
			set => Parent = value;
		}

		protected IList<T> Children => _elements;

		protected virtual string Separator => null;

		protected abstract void OffsetElement(ref PointF location, ref SizeF size, SizeF elementSize);

		int IDocumentElement.Start
		{
			get => Start;
			set => Start = value;
		}

		public string Text
		{
			get
			{
				if (Separator != null)
					return string.Join(Separator, _elements.Select(r => r.Text));
				return string.Concat(_elements.Select(r => r.Text));
			}
			set
			{
				_elements.Clear();
				if (!string.IsNullOrEmpty(value))
				{
					if (Separator != null)
					{
						var lines = value.Split(new[] { Separator }, StringSplitOptions.None);
						foreach (var line in lines)
						{
							var element = new T();
							element.Text = line;
							element.Parent = this;
							_elements.Add(element);
						}
					}
					else
					{
						var element = new T();
						element.Text = value;
						element.Parent = this;
						_elements.Add(element);
					}
				}
				MeasureIfNeeded();
			}
		}

		internal abstract DocumentElement<T> Create();

		public IEnumerator<T> GetEnumerator() => _elements.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public SizeF Measure(SizeF availableSize, PointF location)
		{
			var size = MeasureOverride(availableSize, location);
			Bounds = new RectangleF(location, size);
			return size;
		}

		protected virtual SizeF MeasureOverride(SizeF availableSize, PointF location)
		{
			SizeF size = SizeF.Empty;
			int index = 0;
			var separatorLength = Separator?.Length ?? 0;
			PointF elementLocation = location;
			for (int i = 0; i < _elements.Count; i++)
			{
				if (i > 0)
					index += separatorLength;
				var element = _elements[i];
				element.Start = index;
				var elementSize = element.Measure(availableSize, elementLocation);

				OffsetElement(ref elementLocation, ref size, elementSize);
				index += element.Length;
			}
			Length = index;
			return size;
		}

		internal T Find(int index)
		{
			foreach (var element in this)
			{
				if (index <= element.Start + element.Length)
					return element;
			}
			return null;
		}

		public void Insert(int index, T element)
		{
			element.Start = index;
			element.Parent = this;
			for (int i = 0; i < _elements.Count; i++)
			{
				if (_elements[i].Start >= index)
				{
					_elements.Insert(i, element);
					Adjust(i, element.Length);
					MeasureIfNeeded();
					return;
				}
			}
			_elements.Add(element);
			Length += element.Length;
			MeasureIfNeeded();
		}

		private void Adjust(int startIndex, int length)
		{
			Length += length;
			for (int j = startIndex + 1; j < _elements.Count; j++)
			{
				_elements[j].Start += length;
			}
		}

		public virtual void Add(T element)
		{
			element.Start = Length;
			element.Parent = this;
			_elements.Add(element);
			Length += element.Length;
			MeasureIfNeeded();
		}

		public virtual void Remove(T element)
		{
			var index = _elements.IndexOf(element);
			if (index == -1)
				return;
			_elements.Remove(element);
			Adjust(index, -element.Length);
			MeasureIfNeeded();
		}

		public virtual void Remove(int index, int length)
		{
			for (int i = 0; i < _elements.Count; i++)
			{
				var element = _elements[i];
				if (element.Start > index || element.End < index)
					continue;
				var start = index - element.Start;
				var end = start + length;
				if (start <= element.Start && end >= element.End)
				{
					Remove(element);
					length -= element.Length;
					continue;
				}

				if (start < element.Start && end >= element.Length)
				{
					var removeLength = element.Length - start;
					element.Remove(start, removeLength);
					length -= removeLength;
				}
				else if (end < element.Length)
				{
					element.Remove(start, length);
					length -= length;
				}
				else if (end > element.End && element is Paragraph paragraph)
				{
					// merge the next paragraph into this one
					if (i + 1 < _elements.Count)
					{
						var nextElement = _elements[i + 1];
						if (nextElement is Paragraph nextParagraph)
						{
							foreach (var child in nextParagraph)
							{
								paragraph.Add(child);
							}
							_elements.Remove(nextElement);
							length--; // newline
						}
					}
				}
				else
				{
					element.Remove(start, length);
					length -= length;
				}
				if (element.Length == 0)
				{
					Remove(element);
				}
				if (length > 0)
				{
					Recalculate(Start);
				}

			}
			MeasureIfNeeded();
		}

		internal virtual void MeasureIfNeeded()
		{
		}

		IDocumentElement IDocumentElement.Split(int index)
		{
			if (index >= Length || Start == index)
				return null;
			DocumentElement<T> CreateNew()
			{
				var newElement = Create();
				newElement.Parent = Parent;
				return newElement;
			}

			DocumentElement<T> newRun = null;
			for (int i = 0; i < _elements.Count; i++)
			{
				var element = _elements[i];
				if (element.Start >= index)
				{
					_elements.Remove(element);
					i--;
					if (newRun == null)
					{}
					newRun ??= CreateNew();
					element.Start = newRun.Length;
					newRun.Add(element);
				}
				else if (element.End > index)
				{
					newRun ??= CreateNew();
					var newElement = (T)element.Split(index - element.Start);
					if (newElement != null)
					{
						newElement.Start = newRun.Length;
						newRun.Add(newElement);
					}
				}
			}
			return newRun;
		}

		void IDocumentElement.Recalculate(int index) => Recalculate(index);
		internal void Recalculate(int index)
		{
			Start = index;
			for (int i = 0; i < _elements.Count; i++)
			{
				if (i > 0)
					index++; // newline
				var element = _elements[i];
				element.Recalculate(index);
				index += element.Length;
			}
			Length = index;
		}
		
		public PointF? GetPointAtIndex(int index)
		{
			var element = Find(index);
			var point = element?.GetPointAtIndex(index - element.Start);
			return point ?? Bounds.Location;
		}
		
        public int GetIndexAtPoint(PointF point)
        {
			if (point.Y < Bounds.Top || point.Y > Bounds.Bottom)
				return -1;
			foreach (var element in this)
			{
				var index = element.GetIndexAtPoint(point);
				if (index >= 0)
					return index + element.Start;
			}
			return Length;
        }
		
	}

	public class Document : DocumentElement<Paragraph>
	{
		internal override DocumentElement<Paragraph> Create() => throw new InvalidOperationException();
		
		public float ParagraphSpacing { get; set; }

        protected override string Separator => "\n";

        protected override void OffsetElement(ref PointF location, ref SizeF size, SizeF elementSize)
        {
			size.Width = Math.Max(size.Width, elementSize.Width);
			
			var height = ParagraphSpacing + elementSize.Height;
			
			size.Height += height;
			location.Y += height;
        }

        int _suspendMeasure;

		public event EventHandler Changed;

		public SizeF Size { get; internal set; }

		public Font DefaultFont { get; set; } = SystemFonts.Default();

		public Brush TextBrush { get; set; } = new SolidBrush(SystemColors.ControlText);
        public WrapMode WrapMode { get; internal set; }

        public void BeginEdit()
		{
			_suspendMeasure++;
		}

		public void EndEdit()
		{
			_suspendMeasure--;
			if (_suspendMeasure == 0)
			{
				MeasureIfNeeded();
			}
		}

		public RectangleF CalculateCaretBounds(int index, Font font, Screen screen)
		{
			var scale = screen?.Scale ?? 1;
			var lineHeight = font.LineHeight * scale;
			var leading = (font.Baseline - font.Ascent) * scale;
			var point = GetPointAtIndex(index) ?? Bounds.Location;
			return new RectangleF(point.X, point.Y + leading, 1, lineHeight);
			
			/*
			var paragraph = Find(index);
			var run = paragraph?.Find(index - paragraph.Start);
			var span = run?.Find(index - paragraph.Start - run.Start);
			if (span != null)
			{
				var len = index - paragraph.Start - run.Start - span.Start;
				if (len <= 0)
				{
					return new RectangleF(span.Bounds.X, span.Bounds.Y + leading, 1, lineHeight);
				}
				var text = span.Text.Substring(0, len);
				var size = span.Font.MeasureString(text);
				return new RectangleF(span.Bounds.X + size.Width, span.Bounds.Y+ leading, 1, lineHeight);
			}
			else if (run != null)
			{
				return new RectangleF(run.Bounds.X, run.Bounds.Y + leading, 1, lineHeight);
			}
			else if (paragraph != null)
			{
				return new RectangleF(paragraph.Bounds.X, paragraph.Bounds.Y + leading, 1, lineHeight);
			}
			else
			{
			}
			return new RectangleF(0, leading, 1, lineHeight);
			*/
		}

		public Font GetFont(int index)
		{
			var paragraph = Find(index);
			var run = paragraph?.Find(index - paragraph.Start);
			var span = run?.Find(index - paragraph.Start - run.Start);
			return span?.Font ?? DefaultFont;
		}

		public void Replace(int index, int length, Span span)
		{
			BeginEdit();
			Remove(index, length);
			Insert(index, span);
			EndEdit();
		}

		public void Insert(int index, string text, Font font = null, Brush brush = null)
		{
			Insert(index, new Span { Text = text, Font = font ?? DefaultFont, Brush = brush ?? TextBrush });
		}

		bool InsertToParagraph(Paragraph paragraph, int index, Span insertSpan)
		{
			if (paragraph == null)
				return false;
			if (insertSpan.Font == null)
				insertSpan.Font = DefaultFont;
			var text = insertSpan.Text;
			var run = paragraph?.Find(index);
			var span = run?.Find(index - run.Start);
			if (span != null)
			{
				if (span.Matches(insertSpan))
				{
					// span matches! just insert the text
					var insertIndex = index - run.Start - span.Start;
					if (insertIndex < 0)
						throw new InvalidOperationException("State of document is invalid");

					span.Text = span.Text.Insert(insertIndex, text);
				}
				else
				{
					var rightSpan = span.Split(index - run.Start - span.Start);
					if (rightSpan != null)
					{
						run.Insert(index - run.Start, rightSpan);
					}

					var newSpan = insertSpan.WithText(text);
					run.Insert(index - run.Start, newSpan);
				}
			}
			else if (run != null)
			{
				var rightRun = run.Split(index - run.Start);
				if (rightRun != null)
				{
					// shouldn't happen?!
					paragraph.Insert(index - paragraph.Start, rightRun);
				}
				run.Add(insertSpan.WithText(text));
			}
			else if (paragraph != null)
			{
				var rightParagraph = paragraph.Split(index - paragraph.Start);
				if (rightParagraph != null)
				{
					var paragraphIndex = Children.IndexOf(paragraph);
					Children.Insert(paragraphIndex + 1, rightParagraph);
				}
				var newRun = new Run { };
				newRun.Insert(index - paragraph.Start, insertSpan.WithText(text));
				paragraph.Insert(index - paragraph.Start, newRun);
			}
			return true;
		}

		public void Insert(int index, Span insertSpan)
		{
			var text = insertSpan.Text;
			var lines = text.Split(new[] { "\n" }, StringSplitOptions.None);
			if (lines.Length == 0)
				return;

			index = Math.Min(index, Length);

			var line = lines[0];
			var paragraph = Find(index);

			if (line.Length > 0)
			{
				// first line
				if (!InsertToParagraph(paragraph, index - paragraph?.Start ?? 0, insertSpan.WithText(line)))
				{
					// couldn't insert, add a new paragraph
					var newSpan = insertSpan.WithText(line);
					var newRun = new Run();
					newRun.Add(newSpan);
					var newParagraph = new Paragraph();
					newParagraph.Add(newRun);
					Add(newParagraph);
				}
			}

			// additional lines in new paragraphs
			for (int i = 1; i < lines.Length; i++)
			{
				line = lines[i];

				paragraph = Find(index);

				// in the middle of a paragraph, split
				var rightParagraph = paragraph?.Split(index - paragraph.Start);
				if (rightParagraph != null)
				{
					var rightParagraphIndex = Children.IndexOf(paragraph) + 1;
					Children.Insert(rightParagraphIndex, rightParagraph);
					// append any text to start of this paragraph
					if (line.Length > 0)
						InsertToParagraph(rightParagraph, 0, insertSpan.WithText(line));
				}
				else
				{
					// newline, create a new paragraph

					var newParagraph = new Paragraph();
					if (line.Length > 0)
					{
						var newRun = new Run();
						newRun.Add(insertSpan.WithText(line));
						newParagraph.Add(newRun);
					}

					Insert(index, newParagraph);
				}

				index += line.Length;
			}
			MeasureIfNeeded();
		}

		internal void Paint(Graphics graphics, RectangleF clipBounds)
		{
			foreach (var paragraph in this)
			{
				if (!paragraph.Bounds.Intersects(clipBounds))
					continue;
				foreach (var run in paragraph)
				{
					if (!run.Bounds.Intersects(clipBounds))
						continue;
						
					foreach (var span in run)
					{
						if (!span.Bounds.Intersects(clipBounds))
							continue;
						span.Paint(graphics, clipBounds);
					}
				}
			}
		}
		internal override void MeasureIfNeeded()
		{
			if (_suspendMeasure == 0)
			{
				Size = Measure(SizeF.PositiveInfinity, PointF.Empty);
				Changed?.Invoke(this, EventArgs.Empty);
			}
		}

		public void Clear()
		{
			Children.Clear();
			MeasureIfNeeded();
		}

		public bool GetIsValid()
		{
			var index = 0;
			if (Children.Count == 0)
				return true;
			for (int i = 0; i < Children.Count; i++)
			{
				if (i > 0)
					index++; // newline
				var paragraph = Children[i];
				if (paragraph.Start != index)
					return false;
				var runIndex = 0;
				foreach (var run in paragraph)
				{
					if (run.Start != runIndex)
						return false;
					var spanIndex = 0;
					foreach (var span in run)
					{
						if (span.Start != spanIndex)
							return false;
						spanIndex += span.Length;
					}
					if (run.Length != spanIndex)
						return false;
					runIndex += run.Length;
				}
				if (paragraph.Length != runIndex)
					return false;
				index += runIndex;
			}
			return true;
		}

		public int Navigate(int index, DocumentNavigationMode type, PointF? caretLocation = null)
		{
			return type switch
			{
				DocumentNavigationMode.PreviousLine => GetPreviousLine(index, caretLocation),
				DocumentNavigationMode.NextLine => GetNextLine(index, caretLocation),
				DocumentNavigationMode.EndOfLine => GetEndOfLine(index),
				DocumentNavigationMode.BeginningOfLine => GetBeginningOfLine(index),
				_ => index
			};
		}

		private int GetBeginningOfLine(int index)
		{
			var paragraph = Find(index);
			if (paragraph == null)
				return 0;
			return paragraph.Start;
		}

		private int GetEndOfLine(int index)
		{
			var paragraph = Find(index);
			if (paragraph == null)
				return Length;
			return paragraph.End;
		}

		int GetNextLine(int index, PointF? caretLocation)
		{
			var paragraph = Find(index);
			if (paragraph == null)
				return Length;
			var row = paragraph.Find(index - paragraph.Start);
			var span = row?.Find(index - paragraph.Start - row.Start);
			if (span != null)
			{
				var point = caretLocation ?? span.GetPointAtIndex(index - paragraph.Start - row.Start - span.Start) ?? PointF.Empty;
				var idx = GetIndexAtPoint(new PointF(point.X, span.Bounds.Bottom + 1));
				if (idx >= 0)
					return idx;
			}
			if (row != null)
			{
				var point = caretLocation ?? row.GetPointAtIndex(index - paragraph.Start - row.Start) ?? PointF.Empty;
				var idx = GetIndexAtPoint(new PointF(point.X, row.Bounds.Bottom + 1));
				if (idx >= 0)
					return idx;
			}
			if (paragraph != null)
			{
				var point = caretLocation ?? paragraph.GetPointAtIndex(index - paragraph.Start) ?? PointF.Empty;
				var idx = GetIndexAtPoint(new PointF(point.X, paragraph.Bounds.Bottom + 1));
				if (idx >= 0)
					return idx;
			}
			return End;

/*
			// TODO: deal with wrapping, deal with different character widths. Index in line may not be right above cursor on next line
			var indexInLine = index - paragraph.Start;
			var paragraphIndex = Children.IndexOf(paragraph);
			if (paragraphIndex == Children.Count - 1)
				return Length;
			var nextParagraph = Children[paragraphIndex + 1];
			// nextParagraph.GetIndexAtPoint()
			return nextParagraph.Start + Math.Min(indexInLine, nextParagraph.Length);
			*/
		}

		int GetPreviousLine(int index, PointF? caretLocation)
		{
			var paragraph = Find(index);
			if (paragraph == null)
				return 0;
				
			var row = paragraph.Find(index - paragraph.Start);
			var span = row?.Find(index - paragraph.Start - row.Start);
			if (span != null)
			{
				var point = caretLocation ?? span.GetPointAtIndex(index - paragraph.Start - row.Start - span.Start) ?? PointF.Empty;
				var idx = GetIndexAtPoint(new PointF(point.X, span.Bounds.Top - 1));
				if (idx >= 0)
					return idx;
			}
			if (row != null)
			{
				var point = caretLocation ?? row.GetPointAtIndex(index - paragraph.Start - row.Start) ?? PointF.Empty;
				var idx = GetIndexAtPoint(new PointF(point.X, row.Bounds.Top - 1));
				if (idx >= 0)
					return idx;
			}
			if (paragraph != null)
			{
				var point = caretLocation ?? paragraph.GetPointAtIndex(index - paragraph.Start) ?? PointF.Empty;
				var idx = GetIndexAtPoint(new PointF(point.X, paragraph.Bounds.Top - 1));
				if (idx >= 0)
					return idx;
			}
			return Start;
			/*
			// TODO: deal with wrapping, deal with different character widths. Index in line may not be right below cursor on previous line
			var indexInLine = index - paragraph.Start;
			var paragraphIndex = Children.IndexOf(paragraph);
			if (paragraphIndex == 0)
				return 0;
			var previousParagraph = Children[paragraphIndex - 1];
			return previousParagraph.Start + Math.Min(indexInLine, previousParagraph.Length);
			*/
		}
	}
	public class Paragraph : DocumentElement<Run>
	{
		internal override DocumentElement<Run> Create() => new Paragraph();

        protected override void OffsetElement(ref PointF location, ref SizeF size, SizeF elementSize)
        {
			// runs are stacked vertically with no space inbetween
			size.Width = Math.Max(size.Width, elementSize.Width);
			size.Height += elementSize.Height;
			location.Y += elementSize.Height;
        }

		protected override SizeF MeasureOverride(SizeF availableSize, PointF location)
		{
			var size = base.MeasureOverride(availableSize, location);
			if (size.Height <= 0 && TopParent is Document doc)
			{
				size.Height = doc.DefaultFont.LineHeight;
			}
			return size;
		}

		internal Paragraph Split(int index) => (Paragraph)((IDocumentElement)this).Split(index);
	}

	public class Run : DocumentElement<Span>
	{
        protected override void OffsetElement(ref PointF location, ref SizeF size, SizeF elementSize)
        {
			// spans are stacked horizontally
			size.Height = Math.Max(size.Height, elementSize.Height);
			size.Width += elementSize.Width;
			location.X += elementSize.Width;
        }

		internal override DocumentElement<Span> Create() => new Run();

		internal Run Split(int index) => (Run)((IDocumentElement)this).Split(index);
	}

	public class Span : IDocumentElement
	{
		FormattedText _text = new FormattedText();

		SizeF? _measureSize;
		Brush _brush;

		public Font Font
		{
			get => _text.Font;
			set
			{
				_text.Font = value;
				_measureSize = null;
			}
		}
		
		protected IDocumentElement Parent { get; set; }
		
		IDocumentElement IDocumentElement.Parent
		{
			get => Parent;
			set => Parent = value;
		}

		public Brush Brush
		{
			get => _brush;
			set => _brush = _text.ForegroundBrush = value;
		}

		public int Start { get; set; }
		public int Length => Text.Length;
		public int End => Start + Length;
		public RectangleF Bounds { get; internal set; }

		public string Text
		{
			get => _text.Text;
			set
			{
				_text.Text = value;
				_measureSize = null;
			}
		}

		public Span? Split(int index)
		{
			if (index >= Length)
				return null;
			var text = Text;
			Text = text.Substring(0, index);
			if (index >= text.Length)
				return null;
			var newSpan = new Span { Text = text.Substring(index), Font = Font };
			return newSpan;
		}

		IDocumentElement IDocumentElement.Split(int index) => Split(index);

		public SizeF Measure(SizeF availableSize, PointF location)
		{
			if (_measureSize == null)
			{
				_text.MaximumWidth = availableSize.Width;
				_measureSize = _text.Measure();
			}
			Bounds = new RectangleF(location, _measureSize.Value);
			return _measureSize.Value;
		}

		internal void Paint(Graphics graphics, RectangleF clipBounds)
		{
			graphics.DrawText(_text, Bounds.Location);
		}

		internal bool Matches(Span insertSpan)
		{
			return Font == insertSpan.Font && Brush == insertSpan.Brush;
		}

		internal Span WithText(string text)
		{
			if (text == Text)
				return this;
			var span = new Span { Font = Font, Brush = Brush, Text = text };
			return span;
		}

		public void Remove(int index, int length)
		{
			var text = Text;
			if (index < 0 || index >= text.Length)
				return;
			if (index + length > text.Length)
				length = text.Length - index;
			Text = text.Remove(index, length);
		}

		public void Recalculate(int index)
		{

		}

        public int GetIndexAtPoint(PointF point)
        {
			if (point.X > Bounds.Right)
				return End;
			if (point.X < Bounds.Left)
				return Start;
			if (point.Y < Bounds.Top || point.Y > Bounds.Bottom)
				return -1;
			var spanX = Bounds.X;
			var spanLength = Length;
			for (int i = 0; i < spanLength; i++)
			{
				var spanSize = Font.MeasureString(Text.Substring(i, 1));
				if (point.X < spanX + spanSize.Width / 2)
					return i;
				spanX += spanSize.Width;
			}
			return Length;
        }

		public PointF? GetPointAtIndex(int index)
		{
			var len = index;
			if (index <= 0)
			{
				return new PointF(Bounds.X, Bounds.Y);
			}
			var text = Text.Substring(0, len);
			var size = Font.MeasureString(text);
			return new PointF(Bounds.X + size.Width, Bounds.Y);

		}
	}
}
