using System;
using Eto.Forms;
using Eto.Drawing;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

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
		Attributes? _defaultAttributes;
		float _screenScale = Screen.PrimaryScreen.Scale;

		public event EventHandler? Changed;

		public SizeF Size { get; internal set; }
		
		internal float ScreenScale
		{
			get => _screenScale;
			set
			{
				if (_screenScale != value)
				{
					_screenScale = value;
					MeasureIfNeeded();
				}
			}
		}

		SizeF _availableSize = SizeF.PositiveInfinity;
		public SizeF AvailableSize
		{
			get => _availableSize;
			set
			{
				if (_availableSize != value)
				{
					_availableSize = value;
					MeasureIfNeeded();
				}
			}
		}
		
		static Font? s_defaultFont;

		internal static Font GetDefaultFont() => s_defaultFont ??= new Font("Arial", SystemFonts.Default().Size);
		
		
		public Attributes DefaultAttributes
		{
			get => _defaultAttributes ??= new Attributes { Font = GetDefaultFont(), ForegroundBrush = new SolidBrush(SystemColors.ControlText) };
			set
			{
				_defaultAttributes = value;
				MeasureIfNeeded();
			}
		}

		public Font DefaultFont
		{
			get => DefaultAttributes.Font ?? GetDefaultFont();
			set => DefaultAttributes.Font = value;
		}
		
		public Brush DefaultForegroundBrush
		{
			get => DefaultAttributes.ForegroundBrush ?? new SolidBrush(SystemColors.ControlText);
			set => DefaultAttributes.ForegroundBrush = value;
		}

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

		public Attributes GetAttributes(int start, int end)
		{
			Attributes? attributes = null;
			foreach (var inline in EnumerateInlines(start, end, false).OfType<SpanElement>())
			{
				if (attributes == null)
					attributes = DefaultAttributes.Merge(inline.Attributes, true);
				else
					attributes.ClearUnmatched(inline.Attributes ?? DefaultAttributes);
			}
			return attributes ?? DefaultAttributes.Clone();
			
		}

		public void Replace(int start, int length, SpanElement span)
		{
			BeginEdit();
			RemoveAt(start, length);
			InsertAt(start, span);
			EndEdit();
		}


		public void InsertText(int start, string text, Attributes? attributes = null)
		{
			InsertAt(start, new SpanElement { Text = text, Attributes = attributes?.Clone() });
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
				InsertElementAt(paragraph, start, false, element);

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
				Size = Measure(DefaultAttributes, AvailableSize, PointF.Empty);
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
			point.Y = line.Bounds.Y;
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
			point.Y = line.Bounds.Y;
			var idx = line.GetIndexAt(point);
			if (idx >= 0)
				return idx + line.DocumentStart;
			if (point.X > line.Bounds.Right)
				return line.DocumentEnd;
			return line.DocumentStart;
		}

		protected override SizeF MeasureOverride(Attributes defaultAttributes, SizeF availableSize, PointF location)
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
				var elementSize = element.Measure(defaultAttributes, availableSize, elementLocation);

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

		internal void SetAttributes(int start, int end, Attributes? attributes)
		{
			foreach (var inline in EnumerateInlines(start, end, false))
			{
				if (inline is not SpanElement span)
					continue;

				var docStart = span.DocumentStart;
				if (start > docStart && start < docStart + span.Length)
				{
					// need to split and apply attributes to right side only
					var right = span.Split(start - docStart);
					if (right != null && span.Parent is IContainerElement container)
					{
						container.InsertAt(span.End, right);
						span = right; // apply new attributes to the right side
						docStart = right.DocumentStart;
						if (end > docStart && end < docStart + span.Length)
						{
							// need to split again as the end is in the middle of the right side
							right = span.Split(end - docStart);	
							if (right != null && span.Parent is IContainerElement container2)
							{
								container2.InsertAt(span.End, right);
							}
						}
					}
				}
				if (end > docStart && end < docStart + span.Length)
				{
					// need to split and apply attributes to left side
					var right = span.Split(end - docStart);
					if (right != null && span.Parent is IContainerElement container)
					{
						container.InsertAt(span.End, right);
					}
				}				
				var existingAttributes = span.Attributes;
				
				Attributes? newAttributes;
				if (existingAttributes != null && attributes != null)
				{
					// need to merge the attributes into a new copy
					newAttributes = existingAttributes.Merge(attributes, true);
				}
				else if (existingAttributes == null)
				{
					// just use a copy of the new attributes
					newAttributes = attributes?.Clone();
				}
				else
				{
					newAttributes = null;
				}
				span.Attributes = newAttributes;
				
				
			}
			MeasureIfNeeded();
		}
	}
}
