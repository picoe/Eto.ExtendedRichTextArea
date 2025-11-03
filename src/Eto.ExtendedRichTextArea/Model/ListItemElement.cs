using Eto.Drawing;
using Eto.Forms;

namespace Eto.ExtendedRichTextArea.Model;

public class ListItemElement : ParagraphElement
{
	protected override ContainerElement<IInlineElement> Create() => new ListItemElement();

	public int Level { get; set; } = 0; // Indentation level for nested lists

	public int Index { get; internal set; }

	float Indent => (Parent as ListElement)?.Type.Indent * (Level + 1) ?? 0;

	public override void Paint(Graphics graphics, RectangleF clipBounds)
	{
		if (Parent is ListElement list)
		{
			var bulletBounds = new RectangleF(
				Bounds.X + list.Type.Indent * Level,
				Bounds.Y,
				list.Type.Indent,
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
				var listIndex = list.Parent?.IndexOf(list) ?? -1;
				if (listIndex >= 0)
				{
					if (Start > 0)
					{
						var right = Parent.Split(End);// + Parent.SeparatorLength);
						if (right != null)
						{
							list.Parent?.Insert(listIndex + 1, right);
						}
						listIndex++;
					}
					list.Remove(this);

					var para = new ParagraphElement();
					para.AddRange(this);
					list.Parent?.Insert(listIndex, para);
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
		var childSize = base.MeasureOverride(defaultAttributes, availableSize, loc);
		childSize.Width += loc.X;
		return childSize;
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
			return $"{list.Type.GetText(this)} {base.GetText()}";
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
			return new ListItemElement { Level = Level };
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
