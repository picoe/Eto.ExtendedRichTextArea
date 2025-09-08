using System;
using Eto.Forms;
using Eto.Drawing;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Collections;
using System.Text;

namespace Eto.ExtendedRichTextArea.Model;

public enum DocumentNavigationMode
{
	NextLine,
	PreviousLine,
	BeginningOfLine,
	EndOfLine,
	NextWord,
	PreviousWord,
}

public class Document : BlockContainerElement<IBlockElement>
{
	protected override ContainerElement<IBlockElement> Create() => new Document();

	protected override IBlockElement CreateElement() => new ParagraphElement();

	public float ParagraphSpacing { get; set; }

	public TabStopsType TabStops { get; set; } = TabStopsType.Fixed(36);

	protected override string Separator => "\n";

	public void Save(DocumentFormat format, Stream stream)
	{
		if (format == null)
			throw new ArgumentNullException(nameof(format));
		if (stream == null)
			throw new ArgumentNullException(nameof(stream));

		if (!format.Save(this, stream))
			throw new InvalidOperationException($"Failed to save document in {format.Name} format.");
	}

	public void Load(DocumentFormat format, Stream stream)
	{
		if (format == null)
			throw new ArgumentNullException(nameof(format));
		if (stream == null)
			throw new ArgumentNullException(nameof(stream));

		if (!format.Load(this, stream))
			throw new InvalidOperationException($"Failed to load document in {format.Name} format.");
	}

	protected override ContainerElement<IBlockElement> Clone()
	{
		var clone = (Document)base.Clone();
		clone.ParagraphSpacing = ParagraphSpacing;
		clone._defaultAttributes = _defaultAttributes?.Clone();
		return clone;
	}

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
		get
		{
			if (_defaultAttributes == null)
			{
				_defaultAttributes = new Attributes { Font = GetDefaultFont(), Foreground = new SolidBrush(SystemColors.ControlText) };
				_defaultAttributes.PropertyChanged += DefaultAttributes_Changed;
			}
			return _defaultAttributes;
		}
		set
		{
			if (_defaultAttributes != null)
				_defaultAttributes.PropertyChanged -= DefaultAttributes_Changed;

			_defaultAttributes = value;
			_defaultAttributes.PropertyChanged += DefaultAttributes_Changed;
			DefaultAttributesChanged?.Invoke(this, EventArgs.Empty);
			MeasureIfNeeded();
		}
	}

	private void DefaultAttributes_Changed(object? sender, PropertyChangedEventArgs e)
	{
		DefaultAttributesChanged?.Invoke(this, EventArgs.Empty);
		MeasureIfNeeded();
	}

	public event EventHandler<EventArgs>? DefaultAttributesChanged;

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
	public event EventHandler<EventArgs>? Changing;

	public DocumentRange GetRange(int start, int end)
	{
		return new DocumentRange(this, start, end);
	}

	public override void BeginEdit()
	{
		base.BeginEdit();

		if (_suspendMeasure == 0)
			Changing?.Invoke(this, EventArgs.Empty);

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
		var lineHeight = (font.Baseline + font.Descent) * scale;
		var point = GetPointAt(start, out var line) ?? Bounds.Location;
		if (line != null)
		{
			point.Y = line.Bounds.Y;
			// lineHeight = line.Baseline * scale;
			if (line.Baseline > 0)
				point.Y += line.Baseline - font.Baseline * scale;
		}

		return new RectangleF(point.X, point.Y, 1, lineHeight);
	}

	public Attributes GetAttributes(int start, int end)
	{
		return GetAttributes(DefaultAttributes, start, end);
	}

	public void Replace(int start, int length, TextElement span)
	{
		BeginEdit();
		RemoveAt(start, length);
		InsertAt(start, span);
		EndEdit();
	}

	public void InsertText(int start, string text, Attributes? attributes = null)
	{
		InsertAt(start, new TextElement { Text = text, Attributes = attributes?.Clone() });
	}

	public override bool InsertAt(int start, IElement element)
	{
		start = Math.Max(0, Math.Min(start, Length));

		BeginEdit();
		var result = base.InsertAt(start, element);
		if (!result && element is IInlineElement inlineElement)
		{
			// if we couldn't insert the inline element, we need to create a new paragraph
			var paragraph = new ParagraphElement();
			Add(paragraph);
			result = paragraph.InsertAt(0, inlineElement);
		}
		EndEdit();
		return result;
	}

	public override void Paint(Graphics graphics, RectangleF clipBounds)
	{
		foreach (var block in this)
		{
			if (!block.Bounds.Intersects(clipBounds))
				continue;
			block.Paint(graphics, clipBounds);
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
		return line == null ? Start : line.Start;
	}

	private int GetEndOfLine(int start)
	{
		var line = EnumerateLines(start).FirstOrDefault();
		return line == null ? End : line.End;
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
			return idx + line.Start;
		if (point.X > line.Bounds.Right)
			return line.End;
		return line.Start;
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
			return idx + line.Start;
		if (point.X > line.Bounds.Right)
			return line.End;
		return line.Start;
	}

	protected override SizeF MeasureOverride(Attributes defaultAttributes, SizeF availableSize, PointF location)
	{
		SizeF size = SizeF.Empty;
		int start = 0;
		var separatorLength = SeparatorLength;
		PointF elementLocation = location;
		for (int i = 0; i < Count; i++)
		{
			if (i > 0)
				start += separatorLength;
			var element = this[i];
			// element.Start = start;
			var elementSize = element.Measure(defaultAttributes, availableSize, elementLocation);

			size.Width = Math.Max(size.Width, elementSize.Width);

			var height = ParagraphSpacing + elementSize.Height;

			size.Height += height;
			elementLocation.Y += height;
			start += element.Length;
		}
		return size;
	}

	public PointF? GetPointAt(int start) => GetPointAt(start, out _);

	public override PointF? GetPointAt(int start, out Line? line)
	{
		line = null;
		var element = FindAt(start).child;
		var point = element?.GetPointAt(start - element.Start, out line);
		return point ?? Bounds.Location;
	}

	public new void SetAttributes(int start, int end, Attributes? attributes)
	{
		BeginEdit();
		base.SetAttributes(start, end, attributes);
		EndEdit();
	}


	public event EventHandler<OverrideAttributesEventArgs>? OverrideAttributes;

	internal void TriggerOverrideAttributes(Line line, Chunk chunk, Attributes attributes, out List<AttributeRange>? newAttributes)
	{
		var args = new OverrideAttributesEventArgs(line, chunk, attributes);
		OverrideAttributes?.Invoke(this, args);
		newAttributes = args.NewAttributes;
	}

	internal float GetNextTabStop(float x) => TabStops?.GetNextTabStop(x) ?? FixedTabStops.DefaultTabStop - (int)x % FixedTabStops.DefaultTabStop;
}
