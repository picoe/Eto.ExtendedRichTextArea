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
			get => _defaultAttributes ??= new Attributes { Font = GetDefaultFont(), Foreground = new SolidBrush(SystemColors.ControlText) };
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

		public Brush DefaultForeground
		{
			get => DefaultAttributes.Foreground ?? new SolidBrush(SystemColors.ControlText);
			set => DefaultAttributes.Foreground = value;
		}

		public WrapMode WrapMode { get; internal set; }

		public DocumentRange GetRange(int start, int end)
		{
			return new DocumentRange(this, start, end);
		}

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
			var point = GetPointAt(start, out var line) ?? Bounds.Location;
			if (line != null)
			{
				point.Y = line.Bounds.Y;
				lineHeight = line.Bounds.Height;
			}
			
			return new RectangleF(point.X, point.Y, 1, lineHeight);
		}

		public Attributes GetAttributes(int start, int end)
		{
			// TODO: move this to ContainerElement?
			bool isRange = end > start;
			Attributes? attributes = null;
			foreach (var paragraph in this)
			{
				if (end < paragraph.Start)// || (end == paragraph.Start && isRange))
					break;
				if (start > paragraph.End || (start == paragraph.End && isRange))
					continue;

				var paragraphAttributes = DefaultAttributes.Merge(paragraph.Attributes, false);

				var paragraphStart = start - paragraph.Start;
				var paragraphEnd = end - paragraph.Start;
				foreach (var inline in paragraph)
				{
					if (paragraphEnd <= inline.Start)// || (paragraphEnd == inline.Start && isRange))
						break;
					if (paragraphStart > inline.End || (paragraphStart == inline.End && isRange))
						continue;
						
					if (attributes == null)
						attributes = paragraphAttributes.Merge(inline.Attributes, true);
					else
						attributes.ClearUnmatched(paragraphAttributes.Merge(inline.Attributes, false));
				}

				if (attributes == null)
					attributes = paragraphAttributes.Clone();
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
				var lines = text.Split(new[] { Separator }, StringSplitOptions.None);
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
						rightParagraph.InsertInParagraph(0, element);
						
						Insert(paragraphIndex + 1, rightParagraph);
						start = rightParagraph.Start + element.Length;
						return start;
					}
				}
				else if (element.Length > 0 && start >= paragraph.Start)
				{
					if (paragraph.InsertInParagraph(start - paragraph.Start, element))
					{
						Adjust(paragraph.Start, element.Length);
						start += element.Length;
						return start;
					}
				}
				else
					return start;
			}

			// create a new paragraph, couldn't insert in existing paragraph or split
			var newParagraph = new ParagraphElement();
			newParagraph.Attributes = GetAttributes(start, start);
			if (element.Length > 0)
			{
				newParagraph.Add(element);
			}
			InsertAt(start, newParagraph);
			start = newParagraph.End;
			return start;
		}

		public void Paint(Graphics graphics, RectangleF clipBounds)
		{
			foreach (var paragraph in this)
			{
				if (!paragraph.Bounds.Intersects(clipBounds))
					continue;
				paragraph.Paint(graphics, clipBounds);
			}
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
				foreach (var inline in paragraph)
				{
					if (inline.Start != runIndex)
						return false;
					runIndex += inline.Length;
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
			foreach (var word in words)
			{
				if (start > word.start + word.text.Length)
					return word.start + Start;
			}
			return Start;
		}


		private int GetNextWord(int start)
		{
			var words = EnumerateWords(start, true);
			foreach (var word in words)
			{
				if (start < word.start)
					return word.start + Start;
			}
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
				point = GetPointAt(start, out _) ?? point;
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
				point = GetPointAt(start, out _) ?? point;
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
		
		public PointF? GetPointAt(int start) => GetPointAt(start, out _);
		
		public override PointF? GetPointAt(int start, out Line? line)
		{
			line = null;
			var element = Find(start);
			var point = element?.GetPointAt(start - element.Start, out line);
			return point ?? Bounds.Location;
		}

		internal void SetAttributes(int start, int end, Attributes? attributes)
		{
			// TODO: move this to ContainerElement
			for (int i = 0; i < Count; i++)
			{
				var paragraph = this[i];
				if (paragraph == null)
					continue;
				if (end <= paragraph.Start)
					break;
				if (start >= paragraph.End)
					continue;

				if (start <= paragraph.Start && end >= paragraph.End)
				{
					// encompasses entire paragraph, apply style to the paragraph itself
					paragraph.Attributes = UpdateAttributes(attributes, paragraph.Attributes);
				}

				var paragraphStart = start - paragraph.Start;
				var paragraphEnd = end - paragraph.Start;
				for (int j = 0; j < paragraph.Count; j++)
				{
					var inline = paragraph[j];
					if (inline == null)
						continue;
					if (paragraphEnd <= inline.Start)
						break;
					if (paragraphStart >= inline.End)
						continue;

					IElement applySpan = inline;
					var docStart = inline.DocumentStart;
					if (start > docStart && start < docStart + inline.Length)
					{
						// need to split and apply attributes to right side only
						var right = inline.Split(start - docStart);
						if (right != null && inline.Parent is IBlockElement container)
						{
							container.InsertAt(inline.End, right);

							applySpan = right; // apply new attributes to the right side
							docStart = right.DocumentStart;
							if (end > docStart && end < docStart + applySpan.Length)
							{
								// need to split again as the end is in the middle of the right side
								right = applySpan.Split(end - docStart);
								if (right != null && applySpan.Parent is IBlockElement container2)
								{
									container2.InsertAt(applySpan.End, right);
								}
							}
						}
					}
					if (end > docStart && end < docStart + applySpan.Length)
					{
						// need to split and apply attributes to left side
						var right = applySpan.Split(end - docStart);
						if (right != null && applySpan.Parent is IBlockElement container)
						{
							container.InsertAt(applySpan.End, right);
						}
					}
					applySpan.Attributes = UpdateAttributes(attributes, applySpan.Attributes);
				}

			}
			MeasureIfNeeded();
		}

		private static Attributes? UpdateAttributes(Attributes? attributes, Attributes? spanAttributes)
		{
			var existingAttributes = spanAttributes;

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

			return newAttributes;
		}
		
		public event EventHandler<OverrideAttributesEventArgs>? OverrideAttributes;

		internal void TriggerOverrideAttributes(Line line, Chunk chunk, Attributes attributes, out List<AttributeRange>? newAttributes)
		{
			var args = new OverrideAttributesEventArgs(line, chunk, attributes);
			OverrideAttributes?.Invoke(this, args);
			newAttributes = args.NewAttributes;
		}
	}

	public class OverrideAttributesEventArgs : EventArgs
	{
		public Line Line { get; }
		public Chunk Chunk { get; }
		public Attributes Attributes { get; }

		public int Start => Chunk.Start;
		public int End => Chunk.End;
		public int Length => Chunk.Length;
		public string Text => Chunk.Element.Text;
		
		List<AttributeRange>? _newAttributes;
		public List<AttributeRange> NewAttributes => _newAttributes ??= new List<AttributeRange>();

		public OverrideAttributesEventArgs(Line line, Chunk chunk, Attributes attributes)
		{
			Line = line;
			Chunk = chunk;
			Attributes = attributes;
		}
	}

	public struct AttributeRange
	{
		int? _end;
		int? _length;
		public int Start { get; set; }
		public int End
		{
			get => _end ?? Start + _length ?? 0;
			set
			{
				_end = value;
				_length = null;
			}
		}
		
		public int Length
		{
			get => _length ?? _end ?? Start - Start;
			set
			{
				_length = value;
				_end = null;
			}
		}
		public Attributes Attributes { get; set; }
		
		public AttributeRange(int start, int end, Attributes attributes)
		{
			Start = start;
			_end = end;
			Attributes = attributes;
		}
	}
}
