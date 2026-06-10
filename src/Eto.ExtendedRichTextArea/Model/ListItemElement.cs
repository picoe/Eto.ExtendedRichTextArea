using Eto.Drawing;
using Eto.Forms;

namespace Eto.ExtendedRichTextArea.Model;

public class ListItemElement : ParagraphElement
{
	public ListItemElement()
	{
	}

	internal ListItemElement(ContainerElement<IInlineElement> paragraph)
	{
		Attributes = paragraph.Attributes;
		AddRange(paragraph);
	}

	protected override ContainerElement<IInlineElement> Create() => new ListItemElement
	{
		Level = Level,
		WrapMode = WrapMode,
		TextAlignment = TextAlignment
	};

	public int Level { get; set; } = 0; // Indentation level for nested lists

	public int Index { get; internal set; }

	// The list indent scales with the item's font size so the bullet/number slot
	// grows with the text. The editor zoom enlarges text by scaling the
	// runs' font sizes; with a fixed indent the (now wider) bullet/number overflows
	// its slot -- TextListType/NumericListType center it in the slot, so a slot
	// narrower than the text yields a negative offset (drawn too far left) and the
	// glyphs spill into the item text (overlap). Type.Indent is the nominal indent
	// at the default font size; clamped so it never shrinks below the nominal.
	float IndentScale
	{
		get
		{
			var defaultSize = Document.GetDefaultFont().Size;
			if (defaultSize <= 0)
				return 1f;
			var size = ActualAttributes?.BaseFont?.Size ?? defaultSize;
			var scale = size / defaultSize;
			return scale < 1f ? 1f : scale;
		}
	}

	float IndentUnit => ((Parent as ListElement)?.Type.Indent ?? 0) * IndentScale;

	float Indent => IndentUnit * (Level + 1);

	public override void Paint(Graphics graphics, RectangleF clipBounds)
	{
		if (Parent is ListElement list)
		{
			var indentUnit = IndentUnit;
			var bulletBounds = new RectangleF(
				Bounds.X + indentUnit * Level,
				Bounds.Y,
				indentUnit,
				Bounds.Height
			);
			list.Type.Paint(this, graphics, bulletBounds);
		}
		base.Paint(graphics, clipBounds);
	}

	protected override void OnKeyDown(int start, int end, KeyEventArgs args)
	{
		base.OnKeyDown(start, end, args);
		if (args.KeyData == Keys.Tab && start == 0)
		{
			var doc = this.GetDocument();
			doc?.BeginEdit();
			Level++;
			doc?.EndEdit();
			args.Handled = true;
		}
		else if
		(
			start == 0 && Level > 0 &&
			(
				args.KeyData == (Keys.Tab | Keys.Shift)
				|| args.KeyData == Keys.Backspace
			)
		)
		{
			var doc = this.GetDocument();
			doc?.BeginEdit();
			Level--;
			doc?.EndEdit();
			args.Handled = true;
		}
		else if (start == 0 && end == 0 && Level == 0 && args.KeyData == Keys.Backspace)
		{
			// Convert list item to top level paragraph
			var doc = this.GetDocument();
			if (doc != null && Parent is ListElement list)
			{
				doc.BeginEdit();
				var parent = list.Parent;
				var listIndex = parent?.IndexOf(list) ?? -1;
				var itemIndex = list.IndexOf(this);
				if (listIndex >= 0 && itemIndex >= 0)
				{
					var listToRemoveFrom = list;
					if (itemIndex > 0)
					{
						var right = Parent.Split(Start) as ListElement;
						if (right != null)
						{
							parent?.Insert(listIndex + 1, right);
							listToRemoveFrom = right;
							listIndex++;
						}
					}
					listToRemoveFrom.Remove(this);
					if (listToRemoveFrom.Count == 0)
						parent?.Remove(listToRemoveFrom);

					var para = new ParagraphElement();
					para.AddRange(this);
					parent?.Insert(listIndex, para);
					args.Handled = true;
				}

				doc.EndEdit();
			}
		}
	}

	protected override SizeF MeasureOverride(Attributes defaultAttributes, SizeF availableSize, PointF location)
	{
		if (Parent is not ListElement list)
			return base.MeasureOverride(defaultAttributes, availableSize, location);

		// TODO: Indent should be based on the font and tabstops, and could be per-row
		// e.g. if the font used for the list text is larger than space allowed,
		// then it should go to the next tab stop.  See word's behaviour for reference.
		var loc = location;
		loc.X += Indent;
		// availableSize.Width -= Indent;
		var childSize = base.MeasureOverride(defaultAttributes, availableSize, loc);
		childSize.Width += loc.X;
		return childSize;
	}

	protected override float AlignOverride(SizeF totalSize)
	{
		totalSize.Width -= Indent;
		return base.AlignOverride(totalSize);
	}

	public override PointF? GetPointAt(int start, out Line? line)
	{
		if (Count == 0 && Parent is ListElement list)
		{
			line = null;
			return Bounds.Location + new PointF(Indent, 0);
		}
		return base.GetPointAt(start, out line);
	}

	protected override string GetText()
	{
		if (Parent is ListElement list)
		{
			// add tabs for indentation, and the bullet text for the list item
			var tabs = new string('\t', Level);
			return $"{tabs}{list.Type.GetText(this)} {base.GetText()}";
		}
		return base.GetText();
	}

	public override bool InsertAt(int start, IElement element)
	{
		if (start == 0 && element is TextElement text && text.Text == "\t")
		{
			Level++;
			return false;
		}

		return base.InsertAt(start, element);
	}

	protected override IElement? Split(int start)
	{
		if (base.Split(start) is ListItemElement rightElement)
		{
			rightElement.Level = Level;
			return rightElement;
		}
		if (start == Length)
		{
			return Create(); 
		}
		return null;
	}

	protected override ContainerElement<IInlineElement> Clone()
	{
		var clone = (ListItemElement)base.Clone();
		clone.Level = Level;
		return clone;
	}
}
