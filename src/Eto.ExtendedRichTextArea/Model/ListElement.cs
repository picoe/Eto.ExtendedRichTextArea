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
	public abstract void Paint(Graphics graphics, RectangleF bounds);
}

public class UnorderedListType : ListType
{
	public string BulletCharacter { get; set; } = "•"; // Default bullet character

	public override void Paint(Graphics graphics, RectangleF bounds)
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

public class ListItemElement : ParagraphElement
{
	internal override ContainerElement<IInlineElement> Create() => new ListItemElement();

	public int Level { get; set; } = 0; // Indentation level for nested lists
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
		location.X += Type.Indent; // Start with indentation for the list
		ListElement list = this;
		for (int i = 0; i < list.Count; i++)
		{
			var child = list[i];
			if (child == null)
				continue;
			var loc = location;
			loc.X += child.Level * Type.Indent; // Further adjust for nested levels
			var childSize = child.Measure(defaultAttributes, availableSize, loc);
			childSize.Width += Type.Indent; // Add indentation to each child's width
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

	public override void Paint(Graphics graphics, RectangleF clipBounds)
	{
		for (int i = 0; i < Count; i++)
		{
			var element = this[i];
			var bulletBounds = new RectangleF(
				element.Bounds.X - Type.Indent,
				element.Bounds.Y,
				Type.Indent,
				element.Bounds.Height
			);
			Type.Paint(graphics, bulletBounds);
			element.Paint(graphics, clipBounds);
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
