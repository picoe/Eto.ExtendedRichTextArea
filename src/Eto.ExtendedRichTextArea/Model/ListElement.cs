using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Eto.Drawing;

namespace Eto.ExtendedRichTextArea.Model;

public abstract class ListType
{
	public float Indent { get; set; } = 20;
	public static ListType Unordered => new UnorderedListType();
	public static ListType Ordered => new OrderedListType();
	public abstract void Paint(ListItemElement list, Graphics graphics, RectangleF bounds);

	public abstract string GetText(int index);
}

public class UnorderedListType : ListType
{
	public string BulletCharacter { get; set; } = "•"; // Default bullet character

	public override string GetText(int index)
	{
		return BulletCharacter + " ";
	}

	public override void Paint(ListItemElement list, Graphics graphics, RectangleF bounds)
	{
		// Draw bullet points for unordered lists
		var font = Fonts.Sans(12);
		var textSize = graphics.MeasureString(font, BulletCharacter);
		var bulletBounds = new RectangleF(
			bounds.X + (bounds.Width - textSize.Width) / 2,
			bounds.Y + (bounds.Height - textSize.Height) / 2,
			textSize.Width,
			textSize.Height
		);
		graphics.DrawText(font, SystemColors.ControlText, bulletBounds.Location, BulletCharacter);
	}
}

public class OrderedListType : ListType
{
	public string NumberFormat { get; set; } = "{0}. "; // Default number format

	public override string GetText(int index)
	{
		return string.Format(NumberFormat, index + 1);
	}

	public override void Paint(ListItemElement list, Graphics graphics, RectangleF bounds)
	{
		// Draw numbers for ordered lists
		var font = Fonts.Sans(12);
		var numberText = string.Format(NumberFormat, list.Index + 1); // Use the child's calculated index for numbering
		var textSize = graphics.MeasureString(font, numberText);
		var numberBounds = new RectangleF(
			bounds.X + (bounds.Width - textSize.Width) / 2,
			bounds.Y + (bounds.Height - textSize.Height) / 2,
			textSize.Width,
			textSize.Height
		);
		graphics.DrawText(font, SystemColors.ControlText, numberBounds.Location, numberText);
	}
}

public class ListItemElement : ParagraphElement
{
	protected override ContainerElement<IInlineElement> Create() => new ListItemElement();

	public int Level { get; set; } = 0; // Indentation level for nested lists

	internal int Index { get; set; }

	float Indent => (Parent as ListElement)?.Type.Indent * (Level + 1) ?? 0;

	public override void Paint(Graphics graphics, RectangleF clipBounds)
	{
		if (Parent is ListElement list)
		{
			var bulletBounds = new RectangleF(
				Bounds.X,
				Bounds.Y,
				list.Type.Indent,
				Bounds.Height
			);
			list.Type.Paint(this, graphics, bulletBounds);
		}
		base.Paint(graphics, clipBounds);
	}

	protected override SizeF MeasureOverride(Attributes defaultAttributes, SizeF availableSize, PointF location)
	{
		if (Parent is not ListElement list)
			return base.MeasureOverride(defaultAttributes, availableSize, location);

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
			return list.Type.GetText(Index) + base.GetText();
		}
		return base.GetText();
	}
}

public class ListElement : BlockContainerElement<ListItemElement>
{
	protected override ContainerElement<ListItemElement> Create() => new ListElement();
	protected override ListItemElement CreateElement() => new ListItemElement();

	public ListType Type { get; set; } = ListType.Unordered;
	protected override string? Separator => "\n";
	public float ItemSpacing { get; set; } = 2;

	protected override SizeF MeasureOverride(Attributes defaultAttributes, SizeF availableSize, PointF location)
	{
		// Measure each child and accumulate their sizes
		SizeF totalSize = SizeF.Empty;
		ListElement list = this;
		for (int i = 0; i < list.Count; i++)
		{
			var child = list[i];
			if (child == null)
				continue;
			child.Index = i; // Set the index for the child
			var childSize = child.Measure(defaultAttributes, availableSize, location);
			totalSize.Width = Math.Max(totalSize.Width, childSize.Width);
			totalSize.Height += childSize.Height + ItemSpacing;
			location.Y += childSize.Height + ItemSpacing;
		}

		return totalSize;
	}

	public override IEnumerable<Line> EnumerateLines(int start, bool forward = true)
	{
		foreach (var line in base.EnumerateLines(start, forward))
		{
			// adjust line for bullet character required height
			yield return line;
		}
	}

	public override bool InsertAt(int start, IElement element)
	{
		var (child, index, position) = FindAt(start);
		if (child != null)
		{
			if (child.Length == 0 && element is TextElement span && span.Text == "\n")
			{
				// If the child is empty, we remove it from the list, and replace it with a paragraph in the parent
				RemoveAt(index);
				// if we're not at the end of the list, split this list into two lists
				if (index < Count)
				{
					var right = Split(start);
					if (right != null)
						Parent?.Insert(Parent.IndexOf(this) + 1, right);
				}
				Parent?.Insert(Parent.IndexOf(this) + 1, new ParagraphElement());
				return true;
			}
		}
		return base.InsertAt(start, element);
	}

	protected override ContainerElement<ListItemElement> Clone()
	{
		if (base.Clone() is not ListElement clone)
			throw new InvalidOperationException("Failed to clone ListElement.");

		clone.Type = Type;
		clone.ItemSpacing = ItemSpacing;
		return clone;
	}

}
