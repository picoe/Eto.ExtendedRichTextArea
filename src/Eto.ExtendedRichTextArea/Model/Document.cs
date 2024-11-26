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
		NextLine,
		PreviousLine,
		BeginningOfLine,
		EndOfLine,
		NextWord,
		PreviousWord,
	}

	public class Document : ContainerElement<ParagraphElement>
	{
		internal override ContainerElement<ParagraphElement> Create() => throw new InvalidOperationException();

		internal override ParagraphElement CreateElement() => new ParagraphElement();

		public float ParagraphSpacing { get; set; }

		protected override string Separator => "\n";

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

		public RectangleF CalculateCaretBounds(int start, Font font, Screen? screen)
		{
			var scale = screen?.Scale ?? 1;
			var lineHeight = font.LineHeight * scale;
			var leading = (font.Baseline - font.Ascent) * scale;
			var point = GetPointAt(start) ?? Bounds.Location;
			return new RectangleF(point.X, point.Y + leading, 1, lineHeight);
		}

		public Font GetFont(int start)
		{
			var paragraph = Find(start);
			var run = paragraph?.Find(start - paragraph.Start);
			if (run == null || paragraph == null) // prevent NRE warning, compiler isn't smart enough..
				return DefaultFont;
			var span = run?.Find(start - paragraph.Start - run.Start) as SpanElement;
			return span?.Font ?? DefaultFont;
		}

		public Brush GetBrush(int start)
		{
			var paragraph = Find(start);
			var run = paragraph?.Find(start - paragraph.Start);
			if (run == null || paragraph == null) // prevent NRE warning, compiler isn't smart enough..
				return DefaultBrush;
			var span = run?.Find(start - paragraph.Start - run.Start) as SpanElement;
			return span?.Brush ?? DefaultBrush;
		}

		public void Replace(int start, int length, SpanElement span)
		{
			BeginEdit();
			RemoveAt(start, length);
			InsertAt(start, span);
			EndEdit();
		}


		public void InsertText(int start, string text, Font? font = null, Brush? brush = null)
		{
			InsertAt(start, new SpanElement { Text = text, Font = font ?? DefaultFont, Brush = brush ?? DefaultBrush });
		}

		public void InsertAt(int start, IInlineElement element)
		{
			start = Math.Min(start, Length);

			if (element is SpanElement insertSpan)
			{
				var text = insertSpan.Text;
				var lines = text.Split(new[] { "\n" }, StringSplitOptions.None);
				if (lines.Length == 0)
					return;

				// additional lines in new paragraphs
				for (int i = 0; i < lines.Length; i++)
				{
					var line = lines[i];

					var paragraph = Find(start);
					var spanToInsert = insertSpan.WithText(line);
					start = InsertElementAt(paragraph, start, i > 0, spanToInsert);
				}
				MeasureIfNeeded();
				return;
			}
			else
			{
				var paragraph = Find(start);
				InsertElementAt(paragraph, start, true, element);

				MeasureIfNeeded();
			}
		}

		private int InsertElementAt(ParagraphElement? paragraph, int start, bool splitParagraph, IInlineElement element)
		{
			if (paragraph != null)
			{
				if (splitParagraph)
				{
					var rightParagraph = paragraph.Split(start - paragraph.Start);
					if (rightParagraph != null)
					{
						var paragraphIndex = IndexOf(paragraph);
						Insert(paragraphIndex + 1, rightParagraph);
						rightParagraph.InsertInParagraph(0, element);
						start += element.Length + 1;
						return start;
					}
				}
				else if (element.Length > 0)
				{
					if (paragraph.InsertInParagraph(start - paragraph.Start, element))
					{
						Recalculate(Start);
						start += element.Length;
						return start;
					}
				}
				else
					return start;
			}

			// create a new paragraph, couldn't insert in existing paragraph or split
			var newParagraph = new ParagraphElement();
			if (element.Length > 0)
			{
				var newRun = new RunElement();
				newRun.Add(element);
				newParagraph.Add(newRun);
			}
			InsertAt(start, newParagraph);
			start += element.Length + 1;
			return start;
		}

		public void Paint(Graphics graphics, RectangleF clipBounds)
		{
			foreach (var paragraph in this)
			{
				if (!paragraph.Bounds.Intersects(clipBounds))
					continue;
				foreach (var run in paragraph)
				{
					if (!run.Bounds.Intersects(clipBounds))
						continue;
					run.Paint(graphics, clipBounds);

				}
			}
			// */
		}
		
		internal override void MeasureIfNeeded()
		{
			if (_suspendMeasure == 0)
			{
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

		public int Navigate(int start, DocumentNavigationMode type, PointF? caretLocation = null)
		{
			return type switch
			{
				DocumentNavigationMode.NextLine => GetNextLine(start, caretLocation),
				DocumentNavigationMode.PreviousLine => GetPreviousLine(start, caretLocation),
				DocumentNavigationMode.BeginningOfLine => GetBeginningOfLine(start),
				DocumentNavigationMode.EndOfLine => GetEndOfLine(start),
				DocumentNavigationMode.NextWord => GetNextWord(start),
				DocumentNavigationMode.PreviousWord => GetPreviousWord(start),
				_ => start
			};
		}

		private int GetPreviousWord(int start)
		{
			var words = EnumerateWords(start, false);
			var prevWord = words.Skip(1).FirstOrDefault();
			if (prevWord.start >= 0)
				return prevWord.start + Start;
			return End;
		}


		private int GetNextWord(int start)
		{
			var words = EnumerateWords(start, true);
			var nextWord = words.Skip(1).FirstOrDefault();
			if (nextWord.start >= 0)
				return nextWord.start + Start;
			return End;
		}

		private int GetBeginningOfLine(int start)
		{
			var line = EnumerateLines(start, false).FirstOrDefault();
			return line == null ? Start : line.DocumentStart;
		}

		private int GetEndOfLine(int start)
		{
			var line = EnumerateLines(start).FirstOrDefault();
			return line == null ? End : line.DocumentEnd;
		}

		int GetNextLine(int start, PointF? caretLocation)
		{
			var line = EnumerateLines(start).Skip(1).FirstOrDefault();
			if (line == null)
				return End;
			var point = line.Bounds.Location;
			if (caretLocation != null)
				point.X = caretLocation.Value.X;
			else
				point = GetPointAt(start) ?? point;
			var idx = line.GetIndexAt(point);
			if (idx >= 0)
				return idx + line.DocumentStart;
			if (point.X > line.Bounds.Right)
				return line.DocumentEnd;
			return line.DocumentStart;
		}

		int GetPreviousLine(int start, PointF? caretLocation)
		{
			var line = EnumerateLines(start, false).Skip(1).FirstOrDefault();
			if (line == null)
				return Start;
			var point = line.Bounds.Location;
			if (caretLocation != null)
				point.X = caretLocation.Value.X;
			else
				point = GetPointAt(start) ?? point;
			var idx = line.GetIndexAt(point);
			if (idx >= 0)
				return idx + line.DocumentStart;
			if (point.X > line.Bounds.Right)
				return line.DocumentEnd;
			return line.DocumentStart;
		}

		protected override SizeF MeasureOverride(SizeF availableSize, PointF location)
		{
			SizeF size = SizeF.Empty;
			int start = 0;
			var separatorLength = Separator?.Length ?? 0;
			PointF elementLocation = location;
			for (int i = 0; i < Count; i++)
			{
				if (i > 0)
					start += separatorLength;
				var element = this[i];
				element.Start = start;
				var elementSize = element.Measure(availableSize, elementLocation);

				size.Width = Math.Max(size.Width, elementSize.Width);

				var height = ParagraphSpacing + elementSize.Height;

				size.Height += height;
				elementLocation.Y += height;
				start += element.Length;
			}
			Length = start;
			return size;
		}
		public override PointF? GetPointAt(int start)
		{
			var element = Find(start);
			var point = element?.GetPointAt(start - element.Start);
			return point ?? Bounds.Location;
		}

	}
}
