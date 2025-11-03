using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Eto.Drawing;
using Eto.Forms;

namespace Eto.ExtendedRichTextArea.Model;

public class ListElement : BlockContainerElement<ListItemElement>
{
	protected override ContainerElement<ListItemElement> Create() => new ListElement { Type = Type, ItemSpacing = ItemSpacing };
	protected override ListItemElement CreateElement() => new ListItemElement();

	public ListType Type { get; set; } = ListType.Unordered;
	protected override string? Separator => "\n";
	public float ItemSpacing { get; set; }

	protected override SizeF MeasureOverride(Attributes defaultAttributes, SizeF availableSize, PointF location)
	{
		// Measure each child and accumulate their sizes
		SizeF totalSize = SizeF.Empty;
		ListElement list = this;
		Stack<int> indexStack = new();
		indexStack.Push(0);
		for (int i = 0; i < list.Count; i++)
		{
			var child = list[i];
			if (child == null)
				continue;

			// manage index stack for nested levels
			while (indexStack.Count <= child.Level)
				indexStack.Push(0);
			if (child.Level < indexStack.Count - 1)
				while (indexStack.Count > child.Level + 1)
					indexStack.Pop();

			// assign index and increment for next item
			child.Index = indexStack.Pop();
			indexStack.Push(child.Index + 1);

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
				var parentIndex = Parent?.IndexOf(this) ?? -1;
				if (start == 0)
				{
					// we're at the start of the list, just insert a paragraph before the list
					Parent?.Insert(parentIndex, new ParagraphElement());
					return true;
                }
				// if we're not at the end of the list, split this list into two lists
				if (start > 0 && index < Count)
				{
					var right = Split(start - SeparatorLength);
					if (right != null)
						Parent?.Insert(parentIndex + 1, right);
				}
				Parent?.Insert(parentIndex + 1, new ParagraphElement());
				return true;
			}
		}
		return base.InsertAt(start, element);
	}
}
