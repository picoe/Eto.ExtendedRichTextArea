using System.Globalization;

using Eto.Drawing;

namespace Eto.ExtendedRichTextArea.Model;

public abstract class ListType
{
	public float Indent { get; set; } = 20;
	public static ListType Unordered => new MultipleListType
	{
		Types =
		{
			new UnorderedListType(),
			new UnorderedListType { BulletCharacter = "◦" },
			new UnorderedListType { BulletCharacter = "▪" }
		}
	};

	public static ListType Ordered => new MultipleListType
	{
		Types =
		{
			new NumericListType(),
			new AlphabeticalListType(),
			new RomanNumeralListType()
		}
	};
	
	public abstract void Paint(ListItemElement item, Graphics graphics, RectangleF bounds);

	public abstract string GetText(ListItemElement item);
}

public class MultipleListType : ListType
{
	public List<ListType> Types { get; } = new();

	public override string GetText(ListItemElement item)
	{
		return Types[item.Level % Types.Count].GetText(item);
	}

	public override void Paint(ListItemElement item, Graphics graphics, RectangleF bounds)
	{
		Types[item.Level % Types.Count].Paint(item, graphics, bounds);
	}
}

public abstract class TextListType : ListType
{
	public override void Paint(ListItemElement list, Graphics graphics, RectangleF bounds)
	{
		// Draw bullet points for unordered lists
		var font = list.ActualAttributes.Font ?? Document.GetDefaultFont();
		var text = GetText(list);
		var textSize = graphics.MeasureString(font, text);
		var bulletBounds = new RectangleF(
			bounds.X + (bounds.Width - textSize.Width) / 2,
			bounds.Y + (bounds.Height - textSize.Height) / 2,
			textSize.Width,
			textSize.Height
		);
		graphics.DrawText(font, SystemColors.ControlText, bulletBounds.Location, text);
	}
}

public class UnorderedListType : TextListType
{
	public string BulletCharacter { get; set; } = "•"; // Default bullet character

	public override string GetText(ListItemElement item)
	{
		return BulletCharacter;
	}
}

public class AlphabeticalListType : TextListType
{
	public string Format { get; set; } = "{0}."; // Default format for letters

	public bool Uppercase { get; set; }

	public override string GetText(ListItemElement item)
	{
		var index = item.Index;
		char letter = (char)('a' + (index % 26));
		if (Uppercase)
			letter = char.ToUpper(letter, CultureInfo.CurrentCulture);
		int repeat = index / 26 + 1;
		var letterString = new string(letter, repeat);
		return string.Format(Format, letterString);
	}
}


public class RomanNumeralListType : TextListType
{
	public string Format { get; set; } = "{0}.";

	public bool Uppercase { get; set; }

	string NumberToRoman(int number)
	{
		if (number <= 0)
			return string.Empty;

		var thousands = new[] { "", "M", "MM", "MMM" };
		var hundreds = new[] { "", "C", "CC", "CCC", "CD", "D", "DC", "DCC", "DCCC", "CM" };
		var tens = new[] { "", "X", "XX", "XXX", "XL", "L", "LX", "LXX", "LXXX", "XC" };
		var units = new[] { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX" };

		var roman = string.Empty;
		roman += thousands[(number / 1000) % 10];
		roman += hundreds[(number / 100) % 10];
		roman += tens[(number / 10) % 10];
		roman += units[number % 10];

		return roman;
	}

	public override string GetText(ListItemElement item)
	{
		var index = item.Index;
		var roman = NumberToRoman(index + 1);
		if (!Uppercase)
			roman = roman.ToLowerInvariant();
		return string.Format(Format, roman);
	}
}

public class NumericListType : ListType
{
	public string Format { get; set; } = "{0}."; // Default format for numbers

	public override string GetText(ListItemElement item)
	{
		return string.Format(Format, item.Index + 1);
	}

	public override void Paint(ListItemElement list, Graphics graphics, RectangleF bounds)
	{
		// Draw numbers for ordered lists
		var font = list.ActualAttributes.Font ?? Document.GetDefaultFont();
		var numberText = GetText(list);
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
