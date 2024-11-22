using System;
using Eto.Forms;
using Eto.Drawing;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;

namespace Eto.ExtendedRichTextArea.Model
{
	public enum DocumentNavigationMode
	{
		PreviousLine,
		NextLine,
		EndOfLine,
		BeginningOfLine
	}
	
	public class Attributes
	{
		public Font? Font { get; set; }
		public Brush? Brush { get; set; }
		
		public bool Underline { get; set; }
		public bool Strikethrough { get; set; }
		
		public float Offset { get; set; }
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

		public event EventHandler? Changed;

		public SizeF Size { get; internal set; }

		public Font DefaultFont { get; set; } = SystemFonts.Default();
		public Brush DefaultBrush { get; set; } = new SolidBrush(SystemColors.ControlText);

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

		public RectangleF CalculateCaretBounds(int index, Font font, Screen? screen)
		{
			var scale = screen?.Scale ?? 1;
			var lineHeight = font.LineHeight * scale;
			var leading = (font.Baseline - font.Ascent) * scale;
			var point = GetPointAtIndex(index) ?? Bounds.Location;
			return new RectangleF(point.X, point.Y + leading, 1, lineHeight);
		}

		public Font GetFont(int index)
		{
			var paragraph = Find(index);
			var run = paragraph?.Find(index - paragraph.Start);
			if (run == null || paragraph == null) // prevent NRE warning, compiler isn't smart enough..
				return DefaultFont;
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


		public void Insert(int index, string text, Font? font = null, Brush? brush = null)
		{
			Insert(index, new Span { Text = text, Font = font ?? DefaultFont, Brush = brush ?? DefaultBrush });
		}

		bool InsertToParagraph(Paragraph? paragraph, int index, Span insertSpan)
		{
			if (paragraph == null)
				return false;
			if (insertSpan.Font == null)
				insertSpan.Font = DefaultFont;
			var text = insertSpan.Text;
			var run = paragraph?.Find(index);
			var span = run?.Find(index - run.Start);
			if (span != null && run != null)
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
			else if (run != null && paragraph != null)
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
				if (rightParagraph != null && paragraph != null)
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
		
		internal IEnumerable<Span> EnumerateSpans(int start, int end)
		{
			var startOffset = 0;
			var endOffset = 0;
			for (int i = 0; i < Children.Count; i++)
			{
				var paragraph = Children[i];
				if (paragraph.Start >= end)
					break;
				if (paragraph.End <= start)
					continue;
				var runStart = start - paragraph.Start;
				var runEnd = end - paragraph.Start;
				foreach (var run in paragraph)
				{
					if (run.Start >= runEnd)
						break;
					if (run.End <= runStart)
						continue;
					var spanStart = runStart - run.Start;
					var spanEnd = runEnd - run.Start;
					foreach (var span in run)
					{
						if (span.Start >= spanEnd)
							break;
						if (span.End <= spanStart)
							continue;
						if (span.Start < spanStart)
							startOffset = spanStart - span.Start;
						if (span.End > spanEnd)
							endOffset = span.End - end;
						yield return span;
					}
				}
			}
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
			if (span != null && row != null)
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
		}

		int GetPreviousLine(int index, PointF? caretLocation)
		{
			var paragraph = Find(index);
			if (paragraph == null)
				return 0;
				
			var row = paragraph.Find(index - paragraph.Start);
			var span = row?.Find(index - paragraph.Start - row.Start);
			if (span != null && row != null)
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
		}
	}
}
