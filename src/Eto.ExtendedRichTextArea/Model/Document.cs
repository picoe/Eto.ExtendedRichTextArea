using System;
using Eto.Forms;
using Eto.Drawing;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using Eto.ExtendedRichTextArea.Measure;

namespace Eto.ExtendedRichTextArea.Model
{
	public enum DocumentNavigationMode
	{
		NextLine,
		PreviousLine,
		BeginningOfLine,
		EndOfLine,
		NextWord,
		PreviousWord,
	}

	public class Attributes
	{
		public Font? Font { get; set; }
		public Brush? Brush { get; set; }

		public bool Underline { get; set; }
		public bool Strikethrough { get; set; }

		public float Offset { get; set; }
	}

	public class Document : Element<Paragraph>
	{
		internal override Element<Paragraph> Create() => throw new InvalidOperationException();

		internal override Paragraph CreateElement() => new Paragraph();

		public float ParagraphSpacing { get; set; }

		protected override string Separator => "\n";

		protected override void OffsetElement(ref PointF location, ref SizeF size, SizeF elementSize)
		{
			size.Width = Math.Max(size.Width, elementSize.Width);

			var height = ParagraphSpacing + elementSize.Height;

			size.Height += height;
			location.Y += height;
		}
		
		public override void OffsetElement(Measurement measurement)
		{
			// move next line below the current one
			var location = measurement.CurrentLine?.Bounds.BottomLeft ?? measurement.CurrentParagraph?.Bounds.BottomLeft ?? PointF.Empty;
			location.Y += ParagraphSpacing;
			measurement.CurrentLocation = location;
		}
		

		int _suspendMeasure;

		public event EventHandler? Changed;

		public SizeF Size { get; internal set; }

		public Font DefaultFont { get; set; } = SystemFonts.Default();
		public Brush DefaultBrush { get; set; } = new SolidBrush(SystemColors.ControlText);

		public WrapMode WrapMode { get; internal set; }

		public override void BeginEdit()
		{
			base.BeginEdit();
			_suspendMeasure++;
		}

		public override void EndEdit()
		{
			base.EndEdit();
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
			var span = run?.Find(index - paragraph.Start - run.Start) as Span;
			return span?.Font ?? DefaultFont;
		}

		public void Replace(int index, int length, Span span)
		{
			BeginEdit();
			RemoveAt(index, length);
			Insert(index, span);
			EndEdit();
		}


		public void InsertText(int index, string text, Font? font = null, Brush? brush = null)
		{
			Insert(index, new Span { Text = text, Font = font ?? DefaultFont, Brush = brush ?? DefaultBrush });
		}

		bool InsertToParagraph(Paragraph? paragraph, int index, Span insertSpan)
		{
			if (paragraph == null)
				return false;
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
					run.Adjust(index - run.Start, text.Length);
				}
				else
				{
					var rightSpan = span.Split(index - run.Start - span.Start);
					if (rightSpan is IInlineElement rightElement)
					{
						run.InsertAt(index - run.Start, rightElement);
					}

					var newSpan = insertSpan.WithText(text);
					run.InsertAt(index - run.Start, newSpan);
					paragraph.Adjust(index, text.Length);
				}
			}
			else if (run != null && paragraph != null)
			{
				var rightRun = run.Split(index - run.Start);
				if (rightRun != null)
				{
					// shouldn't happen?!
					paragraph.InsertAt(index - paragraph.Start, rightRun);
				}
				run.Add(insertSpan.WithText(text));
			}
			else if (paragraph != null)
			{
				var rightParagraph = paragraph.Split(index - paragraph.Start);
				if (rightParagraph != null)
				{
					var paragraphIndex = IndexOf(paragraph);
					InsertAt(paragraphIndex + 1, rightParagraph);
				}
				var newRun = new Run { };
				newRun.InsertAt(index - paragraph.Start, insertSpan.WithText(text));
				paragraph.InsertAt(index - paragraph.Start, newRun);
			}
			return true;
		}

		public void Insert(int index, IInlineElement element)
		{
			var paragraph = Find(index);
			if (paragraph == null)
				return;
			var run = paragraph?.Find(index);
			var span = run?.Find(index - run.Start);
			if (span != null && run != null)
			{
				if (!span.Merge(index - run.Start - span.Start, element))
				{
					var rightSpan = span.Split(index - run.Start - span.Start);
					if (rightSpan is IInlineElement rightElement)
					{
						run.InsertAt(index - run.Start, rightElement);
					}
					run.InsertAt(index - run.Start, element);
				}
			}
			else if (run != null && paragraph != null)
			{
				var rightRun = run.Split(index - run.Start);
				if (rightRun != null)
				{
					// shouldn't happen?!
					paragraph.InsertAt(index - paragraph.Start, rightRun);
				}
				run.Add(element);
			}
			else if (paragraph != null)
			{
				var rightParagraph = paragraph.Split(index - paragraph.Start);
				if (rightParagraph != null)
				{
					var paragraphIndex = IndexOf(paragraph);
					InsertAt(paragraphIndex + 1, rightParagraph);
				}
				var newRun = new Run { };
				newRun.InsertAt(index - paragraph.Start, element);
				paragraph.InsertAt(index - paragraph.Start, newRun);
			}
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
					index += line.Length + 1;
				}
				else
				{
					index += line.Length;
				}
			}

			// additional lines in new paragraphs
			for (int i = 1; i < lines.Length; i++)
			{
				line = lines[i];

				// newline, create a new paragraph
				var newParagraph = new Paragraph();
				if (line.Length > 0)
				{
					var newRun = new Run();
					newRun.Add(insertSpan.WithText(line));
					newParagraph.Add(newRun);
				}
				InsertAt(index, newParagraph);
				index += line.Length + 1;
			}
			MeasureIfNeeded();
		}

		public void Paint(Graphics graphics, RectangleF clipBounds)
		{
			// if (_measurement == null)
			// 	MeasureIfNeeded();
				
			// _measurement?.Paint(graphics, clipBounds);
			
			// /*
			
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
			// */
		}
		Measurement? _measurement;
		
		internal override void MeasureIfNeeded()
		{
			if (_suspendMeasure == 0)
			{
				// _measurement = new Measurement(this, SizeF.PositiveInfinity);
				// _measurement.Measure();

				// Size = _measurement.Bounds.Size;
				
				Size = Measure(SizeF.PositiveInfinity, PointF.Empty);
				Changed?.Invoke(this, EventArgs.Empty);
			}
		}

		public bool GetIsValid()
		{
			var index = 0;
			if (Count == 0)
				return true;
			for (int i = 0; i < Count; i++)
			{
				if (i > 0)
					index++; // newline
				var paragraph = this[i];
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
				DocumentNavigationMode.NextLine => GetNextLine(index, caretLocation),
				DocumentNavigationMode.PreviousLine => GetPreviousLine(index, caretLocation),
				DocumentNavigationMode.BeginningOfLine => GetBeginningOfLine(index),
				DocumentNavigationMode.EndOfLine => GetEndOfLine(index),
				DocumentNavigationMode.NextWord => GetNextWord(index),
				DocumentNavigationMode.PreviousWord => GetPreviousWord(index),
				_ => index
			};
		}

		private int GetPreviousWord(int index)
		{
			return index;
		}


		private int GetNextWord(int index)
		{
			var words = EnumerateWords(index, true);
			var nextWord = words.Skip(1).FirstOrDefault();
			if (nextWord.index >= 0)
				return nextWord.index + Start;
			return End;
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
			// var inlines = EnumerateInlines(index, Length);
			// RectangleF? initialBounds = null;
			// foreach (var inline in inlines)
			// {
			// 	if (initialBounds == null)
			// 	{
			// 		initialBounds = inline.Bounds;
			// 	}
			// 	else if (inline.Bounds.Y > initialBounds.Value.Y)
			// 	{
			// 		var point = inline.Bounds.Location;
			// 		if (caretLocation != null)
			// 		{
			// 			point.X = caretLocation.Value.X;
			// 		}
			// 		this.GetIndexAtPoint(point);
			// 		return inline.Start;
			// 	}
			// }




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
