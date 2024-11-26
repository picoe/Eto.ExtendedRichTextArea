
using Eto.Drawing;

namespace Eto.ExtendedRichTextArea.Model
{
	public class Attributes
	{
		// TODO: split out Font to its different attributes. 
		// e.g. Font family, typeface, underline, etc, then create font based on those
		public Font? Font { get; set; }
		public Brush? ForegroundBrush { get; set; }
		public bool? Underline { get; set; }
		public bool? Strikethrough { get; set; }
		public float? Offset { get; set; }
		
		public Attributes Clone()
		{
			return new Attributes
			{
				Font = Font,
				ForegroundBrush = ForegroundBrush,
				Underline = Underline,
				Strikethrough = Strikethrough,
				Offset = Offset
			};
		}
		
		public override bool Equals(object? obj)
		{
			if (obj == null || obj is not Attributes other)
				return false;

			if (ReferenceEquals(this, obj))
				return true;
				
			return Font == other.Font 
				&& ForegroundBrush == other.ForegroundBrush 
				&& Underline == other.Underline 
				&& Strikethrough == other.Strikethrough 
				&& Offset == other.Offset;
		}
		
		public static bool operator ==(Attributes? attributes1, Attributes? attributes2)
		{
			if (ReferenceEquals(attributes1, null) && ReferenceEquals(attributes2, null))
				return true;
			return attributes1?.Equals(attributes2) == true;
		}

		public static bool operator !=(Attributes? attributes1, Attributes? attributes2)
		{
			if (ReferenceEquals(attributes1, null) && ReferenceEquals(attributes2, null))
				return false;
			return attributes1?.Equals(attributes2) == false;
		}
		
		public override int GetHashCode()
		{
			return HashCode.Combine(Font, ForegroundBrush, Underline, Strikethrough, Offset);
		}
		
		public void Apply(FormattedText formattedText)
		{
			if (Font != null)
				formattedText.Font = Font;
			if (ForegroundBrush != null)
				formattedText.ForegroundBrush = ForegroundBrush;
		}
	}
}
