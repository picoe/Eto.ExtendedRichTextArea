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
		var font = list.ActualAttributes.Font ?? Document.GetDefaultFont();
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
		var font = list.ActualAttributes.Font ?? Document.GetDefaultFont();
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
