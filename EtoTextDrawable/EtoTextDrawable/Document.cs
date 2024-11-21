using System;
using Eto.Forms;
using Eto.Drawing;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace EtoTextDrawable
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
		int Start { get; internal set; }
		int Length { get; }
		int End { get; }
		RectangleF Bounds { get; }
		string Text { get; }
		IDocumentElement Split(int index);
		SizeF Measure(SizeF availableSize, PointF location);
		void Remove(int index, int length);
		void Recalculate(int index);
	}

	public abstract class DocumentElement<T> : IEnumerable<T>, IDocumentElement
		where T : class, IDocumentElement, new()
	{
		List<T> _elements = new List<T>();
		public int Start { get; internal set; }
		public int Length { get; internal set; }
		public int End => Start + Length;
		public RectangleF Bounds { get; internal set; }

		protected IList<T> Children => _elements;

		int IDocumentElement.Start
		{
			get => Start;
			set => Start = value;
		}

		public string Text
		{
			get => string.Concat(_elements.Select(r => r.Text));
			set => SetText(value);
		}

		protected abstract void SetText(string text);
		internal abstract DocumentElement<T> Create();

		public IEnumerator<T> GetEnumerator() => _elements.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public SizeF Measure(SizeF availableSize, PointF location)
		{
			SizeF size = SizeF.Empty;
			int index = 0;
			PointF elementLocation = location;
			foreach (var element in this)
			{
				element.Start = index;
				var spanSize = element.Measure(availableSize, elementLocation);
				size.Width += spanSize.Width;
				size.Height = Math.Max(size.Height, spanSize.Height);

				elementLocation.X += spanSize.Width;
				index += element.Length;
			}
			Bounds = new RectangleF(location, size);
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
			for (int i = 0; i < _elements.Count; i++)
			{
				if (_elements[i].Start > index)
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
				var run = _elements[i];
				if (run.Start > index)
					continue;
				var start = index - run.Start;
				var end = start + length;
				if (start <= run.Start && end >= run.End)
				{
					Remove(run);
					length -= run.Length;
					continue;
				}

				if (start < run.Start && end >= run.Length)
				{
					var removeLength = run.Length - start;
					run.Remove(start, removeLength);
					length -= removeLength;
				}
				else if (end < run.Length)
				{
					run.Remove(start, length);
					length -= length;
				}
				else
				{
					run.Remove(start, length);
					length -= length;
				}
				if (run.Length == 0)
				{
					Remove(run);
				}
			}
			MeasureIfNeeded();
		}

		internal virtual void MeasureIfNeeded()
		{
		}

		IDocumentElement IDocumentElement.Split(int index)
		{
			if (index >= Length)
				return null;

			DocumentElement<T> newRun = null;
			for (int i = 0; i < _elements.Count; i++)
			{
				var element = _elements[i];
				if (element.Start >= index)
				{
					_elements.Remove(element);
					i--;
					element.Start = newRun.Length;
					newRun.Add(element);
				}
				else if (element.End > index)
				{
					newRun = Create();
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

		void IDocumentElement.Recalculate(int index)
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
	}

	public class Document : DocumentElement<Paragraph>
	{
		internal override DocumentElement<Paragraph> Create() => throw new InvalidOperationException();

		protected override void SetText(string text)
		{
			Children.Clear();
			Children.Add(new Paragraph { Text = text });
		}

		int _suspendMeasure;

		public event EventHandler Changed;

		public SizeF Size { get; internal set; }

		public Font DefaultFont { get; set; } = SystemFonts.Default();

		public Brush TextBrush { get; set; } = new SolidBrush(SystemColors.ControlText);

		// public override string Text => string.Concat(Children.Select(p => p.Text + "\n"));

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

		public RectangleF CalculateCaretBounds(int index, Font font)
		{
			var paragraph = FindParagraph(index);
			var run = paragraph?.Find(index - paragraph.Start);
			var span = run?.Find(index - paragraph.Start - run.Start);
			var lineHeight = font.LineHeight;
			if (span != null)
			{
				var len = index - paragraph.Start - run.Start - span.Start;
				if (len <= 0)
				{
					return new RectangleF(span.Bounds.X, span.Bounds.Y, 1, lineHeight);
				}
				var text = span.Text.Substring(0, len);
				var size = span.Font.MeasureString(text);
				return new RectangleF(span.Bounds.X + size.Width, span.Bounds.Y, 1, lineHeight);
			}
			else if (run != null)
			{
				return new RectangleF(run.Bounds.X, run.Bounds.Y, 1, lineHeight);
			}
			else if (paragraph != null)
			{
				return new RectangleF(paragraph.Bounds.X, paragraph.Bounds.Y, 1, lineHeight);
			}
			else
			{
			}
			return new RectangleF(0, 0, 1, lineHeight);
		}

		public Font GetFont(int index)
		{
			var paragraph = FindParagraph(index);
			var run = paragraph?.Find(index - paragraph.Start);
			var span = run?.Find(index - paragraph.Start - run.Start);
			return span?.Font ?? DefaultFont;
		}

		/*
		public void Remove(int index, int length)
		{
			if (length <= 0)
				return;

			while (length > 0)
			{
				var paragraph = FindParagraph(index);
				var run = paragraph?.Find(index - paragraph.Start);
				// var span = run?.FindSpan(index - paragraph.Start - run.Start);
				// 	else
				// 	{
				// 		// merge paragraphs!
				// 		var nextParagraph = FindParagraph(index + 1);
				// 		if (nextParagraph != null && nextParagraph != paragraph)
				// 		{
				// 			foreach (var nextRun in nextParagraph)
				// 			{
				// 				paragraph.Add(nextRun);
				// 			}
				// 			_paragraphs.Remove(nextParagraph);
				// 			length--; // newline
				// 		}
				// 		else
				// 			length = 0;
				// 	}
				// }
				// else 
				if (run != null)
				{
					paragraph.Remove(index - paragraph.Start, length);
					
					if (paragraph.Length == 0)
					{
						Remove(paragraph);
						length--; // newline
					}
				}
				else if (paragraph != null)
				{
					var start = index - paragraph.Start;
					var end = start + length;
					if (start <= paragraph.Start && end >= paragraph.Length)
					{
						Remove(paragraph);
						length--; // newline
						length -= paragraph.Length;
					}
					else if (start < paragraph.Length && end >= paragraph.Length)
					{
						var removeLength = paragraph.Length - start;
						paragraph.Remove(start, removeLength);
						length -= removeLength;
					}
					else if (end < paragraph.Length)
					{
						paragraph.Remove(start, length);
						length -= length;
					}
					else
					{
						paragraph.Remove(start, length);
						length = 0;
					}
				}
				else
				{
					length = 0;
				}
				
				if (length > 0)
				{
					Recalculate();
				}
			}
			MeasureIfNeeded();
		}*/

		public void Replace(int index, int length, Span span)
		{
			BeginEdit();
			Remove(index, length);
			Insert(index, span);
			EndEdit();
		}

		public void Insert(int index, string text, Font font = null, Brush brush = null)
		{
			Insert(index, new Span { Text = text, Font = font, Brush = brush ?? TextBrush });
		}

		bool InsertToParagraph(Paragraph paragraph, int index, Span insertSpan)
		{
			if (paragraph == null)
				return false;
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
				var newRun = new Run { Start = index - paragraph.Start };
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
			var paragraph = FindParagraph(index);

			if (line.Length > 0)
			{
				// first line
				if (!InsertToParagraph(paragraph, index - paragraph?.Start ?? 0, insertSpan.WithText(line)))
				{
					// couldn't insert, add a new paragraph
					var newSpan = insertSpan.WithText(line);
					var newRun = new Run { Start = index };
					newRun.Insert(index, newSpan);
					var newParagraph = new Paragraph { Start = index };
					newParagraph.Add(newRun);
					Add(newParagraph);
				}
			}

			// additional lines in new paragraphs
			for (int i = 1; i < lines.Length; i++)
			{
				line = lines[i];

				paragraph = FindParagraph(index);

				// in the middle of a paragraph, split
				var rightParagraph = paragraph?.Split(index - paragraph.Start);
				if (rightParagraph != null)
				{
					var rightParagraphIndex = Children.IndexOf(paragraph) + 1;
					Children.Insert(rightParagraphIndex, rightParagraph);
					// append any text to start of this paragraph
					InsertToParagraph(rightParagraph, 0, insertSpan.WithText(line));
				}
				else
				{
					// newline, create a new paragraph

					var newParagraph = new Paragraph { Start = index };
					if (line.Length > 0)
					{
						var newRun = new Run();
						newRun.Add(insertSpan.WithText(line));
						newParagraph.Add(newRun);
					}

					var paragraphIndex = paragraph != null ? Children.IndexOf(paragraph) + 1 : Children.Count;
					if (paragraph != null && index == paragraph.Start)
						paragraphIndex--;
					Children.Insert(paragraphIndex, newParagraph);
				}

				index += line.Length;
			}
			MeasureIfNeeded();
		}

		public Paragraph FindParagraph(int index)
		{
			foreach (var paragraph in this)
			{
				if (index <= paragraph.Start + paragraph.Length)
					return paragraph;
			}
			return null;
		}

		internal void Paint(Graphics graphics)
		{
			foreach (var paragraph in this)
			{
				foreach (var run in paragraph)
				{
					foreach (var span in run)
					{
						span.Paint(graphics);
					}
				}
			}
		}
		internal override void MeasureIfNeeded()
		{
			if (_suspendMeasure == 0)
			{
				Measure(SizeF.PositiveInfinity, PointF.Empty);
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

		public int GetIndexAtPoint(PointF point)
		{
			var y = point.Y;
			foreach (var paragraph in this)
			{
				if (y < paragraph.Bounds.Bottom)
				{
					var x = point.X;
					foreach (var run in paragraph)
					{
						if (x < run.Bounds.Right)
						{
							var runY = run.Bounds.Y;
							foreach (var span in run)
							{
								if (y >= runY && y < runY + span.Bounds.Height)
								{
									var spanX = span.Bounds.X;
									var spanWidth = span.Bounds.Width;
									var spanLength = span.Length;
									var spanIndex = span.Start;
									while (spanIndex < spanLength)
									{
										var spanSize = span.Font.MeasureString(span.Text.Substring(spanIndex, 1));
										if (x < spanX + spanSize.Width / 2)
											break;
										spanX += spanSize.Width;
										spanIndex++;
									}
									return paragraph.Start + run.Start + spanIndex;
								}
								runY += span.Bounds.Height;
							}
							return paragraph.Start + run.Start;
						}
					}
					return paragraph.Start;
				}
				y -= paragraph.Bounds.Height;
			}
			return Length;
		}

		public int Navigate(int index, DocumentNavigationMode type)
		{
			return type switch
			{
				DocumentNavigationMode.PreviousLine => GetPreviousLine(index),
				DocumentNavigationMode.NextLine => GetNextLine(index),
				DocumentNavigationMode.EndOfLine => GetEndOfLine(index),
				DocumentNavigationMode.BeginningOfLine => GetBeginningOfLine(index),
				_ => index
			};
		}

		private int GetBeginningOfLine(int index)
		{
			var paragraph = FindParagraph(index);
			if (paragraph == null)
				return 0;
			return paragraph.Start;
		}

		private int GetEndOfLine(int index)
		{
			var paragraph = FindParagraph(index);
			if (paragraph == null)
				return Length;
			return paragraph.End;
		}

		int GetNextLine(int index)
		{
			var paragraph = FindParagraph(index);
			if (paragraph == null)
				return Length;

			// TODO: deal with wrapping, deal with different character widths. Index in line may not be right above cursor on next line
			var indexInLine = index - paragraph.Start;
			var paragraphIndex = Children.IndexOf(paragraph);
			if (paragraphIndex == Children.Count - 1)
				return Length;
			var nextParagraph = Children[paragraphIndex + 1];
			return nextParagraph.Start + Math.Min(indexInLine, nextParagraph.Length);
		}

		int GetPreviousLine(int index)
		{
			var paragraph = FindParagraph(index);
			if (paragraph == null)
				return 0;
			// TODO: deal with wrapping, deal with different character widths. Index in line may not be right below cursor on previous line
			var indexInLine = index - paragraph.Start;
			var paragraphIndex = Children.IndexOf(paragraph);
			if (paragraphIndex == 0)
				return 0;
			var previousParagraph = Children[paragraphIndex - 1];
			return previousParagraph.Start + Math.Min(indexInLine, previousParagraph.Length);
		}
	}
	public class Paragraph : DocumentElement<Run>
	{
		internal override DocumentElement<Run> Create() => new Paragraph();

		protected override void SetText(string text)
		{
			Children.Clear();
			Children.Add(new Run { Text = text });
		}

		internal Paragraph Split(int index) => (Paragraph)((IDocumentElement)this).Split(index);
	}

	public class Run : DocumentElement<Span>
	{
		protected override void SetText(string text)
		{
			Children.Clear();
			Children.Add(new Span { Text = text });
		}

		internal override DocumentElement<Span> Create() => new Run();

		internal Run Split(int index) => (Run)((IDocumentElement)this).Split(index);
	}

	public class Span : IDocumentElement
	{
		FormattedText _text = new FormattedText();

		public Font Font
		{
			get => _text.Font;
			set => _text.Font = value;
		}

		public Brush Brush
		{
			get => _text.ForegroundBrush;
			set => _text.ForegroundBrush = value;
		}

		public int Start { get; set; }
		public int Length => Text.Length;
		public int End => Start + Length;
		public RectangleF Bounds { get; internal set; }

		public string Text
		{
			get => _text.Text;
			set => _text.Text = value;
		}

		public Span Split(int index)
		{
			if (index >= Length)
				return null;
			var newSpan = new Span { Text = Text.Substring(index), Font = Font };
			Text = Text.Substring(0, index);
			return newSpan;
		}

		IDocumentElement IDocumentElement.Split(int index) => Split(index);

		public SizeF Measure(SizeF availableSize, PointF location)
		{
			_text.MaximumWidth = availableSize.Width;
			var size = _text.Measure();
			Bounds = new RectangleF(location, size);
			return size;
		}

		internal void Paint(Graphics graphics)
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

		}

		public void Recalculate(int index)
		{

		}
	}
}
