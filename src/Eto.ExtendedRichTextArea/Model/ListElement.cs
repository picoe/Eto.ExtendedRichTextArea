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
}

public class UnorderedListType : ListType
{
	public string BulletCharacter { get; set; } = "•"; // Default bullet character

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
		graphics.DrawText(font, Brushes.Black, bulletBounds.Location, BulletCharacter);
	}
}

public class OrderedListType : ListType
{
	public string NumberFormat { get; set; } = "{0}. "; // Default number format

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
		graphics.DrawText(font, Brushes.Black, numberBounds.Location, numberText);
	}
}

public class ListItemElement : ParagraphElement
{
	internal override ContainerElement<IInlineElement> Create() => new ListItemElement();

	public int Level { get; set; } = 0; // Indentation level for nested lists
	
	internal int Index { get; set; }

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
		loc.X += (Level + 1) * list.Type.Indent; // Adjust for level
		var childSize = base.MeasureOverride(defaultAttributes, availableSize, loc);
		childSize.Width += loc.X;
		return childSize;
	}
}

public class ListElement : BlockContainerElement<ListItemElement>
{
	internal override ContainerElement<ListItemElement> Create() => new ListElement();
	internal override ListItemElement CreateElement() => new ListItemElement();

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
			if (child.Length == 0 && element is SpanElement span && span.Text == "\n")
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

}
