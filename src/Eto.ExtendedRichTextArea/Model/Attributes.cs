

using System.ComponentModel;
using System.Runtime.CompilerServices;

using Eto.Drawing;

namespace Eto.ExtendedRichTextArea.Model
{
	public class Attributes : INotifyPropertyChanged
	{
		// TODO: split out Font to its different attributes. 
		// e.g. Font family, typeface, underline, etc, then create font based on those
		Font? _font;
		FontFamily? _family;
		FontTypeface? _typeface;
		Brush? _foreground;
		Brush? _background;
		bool? _underline;
		bool? _strikethrough;
		float? _offset;
		float? _size;
		
		public bool IsEmpty => _family == null && _typeface == null && _foreground == null && _background == null && _underline == null && _strikethrough == null && _offset == null;

		public event PropertyChangedEventHandler? PropertyChanged;
		
		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public Font? Font
		{
			get => _font ??= CreateFont();
			set
			{
				_font = value;
				_family = value?.Family;
				_typeface = value?.Typeface;
				_size = value?.Size;
				_underline = value?.FontDecoration.HasFlag(FontDecoration.Underline);
				_strikethrough = value?.FontDecoration.HasFlag(FontDecoration.Strikethrough);
				OnPropertyChanged(nameof(Family));
				OnPropertyChanged(nameof(Typeface));
				OnPropertyChanged(nameof(Size));
				OnPropertyChanged(nameof(Underline));
				OnPropertyChanged(nameof(Strikethrough));
				OnPropertyChanged();
			}
		}

		internal float? Baseline
		{
			get
			{
				var baseline = Font?.Baseline;
				if (baseline == null)
					return null;
				if (Platform.Instance.IsWpf)
					baseline *= 96f / 72f;
				return baseline.Value;
			}
		}

		public float? Size
		{
			get => _size;
			set
			{
				_size = value;
				_font = null;
				OnPropertyChanged();
				OnPropertyChanged(nameof(Font));
			}
		}
		
		public FontFamily? Family
		{
			get => _family ?? _typeface?.Family;
			set
			{
				if (_family == value)
					return;
				_family = value;
				_font = null;
				_typeface = null;
				OnPropertyChanged();
				OnPropertyChanged(nameof(Typeface));
				OnPropertyChanged(nameof(Font));
			}
		}
		
		public FontTypeface? Typeface
		{
			get => _typeface;
			set
			{
				if (_typeface == value)
					return;
				_typeface = value;
				_font = null;
				_family = null;
				OnPropertyChanged();
				OnPropertyChanged(nameof(Family));
				OnPropertyChanged(nameof(Font));
			}
		}

		private Font? CreateFont()
		{
			if (_family == null && _typeface == null)
				return Document.GetDefaultFont();
			var typeface = _typeface ?? _family?.Typefaces.FirstOrDefault();

			var decoration = FontDecoration.None; 
			if (_underline == true)
				decoration |= FontDecoration.Underline;
			if (_strikethrough == true)
				decoration |= FontDecoration.Strikethrough;
			return new Font(typeface, _size ?? 12, decoration);
		}

		public Brush? Foreground
		{
			get => _foreground;
			set
			{
				_foreground = value;
				OnPropertyChanged();
			}
		}

		public Brush? Background
		{
			get => _background;
			set
			{
				_background = value;
				OnPropertyChanged();
			}
		}
		
		public bool? Underline
		{
			get => _underline;
			set
			{
				_underline = value;
				_font = null;
				OnPropertyChanged();
				OnPropertyChanged(nameof(Font));
			}
		}
		public bool? Strikethrough
		{
			get => _strikethrough;
			set
			{
				_strikethrough = value;
				_font = null;
				OnPropertyChanged();
				OnPropertyChanged(nameof(Font));
			}
		}
		public float? Offset
		{
			get => _offset;
			set
			{
				_offset = value;
				OnPropertyChanged();
			}
		}
		
		public Attributes Clone()
		{
			return new Attributes
			{
				_family = _family,
				_typeface = _typeface,
				_size = _size,
				_foreground = _foreground,
				_background = _background,
				_underline = _underline,
				_strikethrough = _strikethrough,
				_offset = _offset
			};
		}
		
		public Attributes Merge(Attributes? attributes, bool copy)
		{
			if (!copy)
			{
				if (attributes == null || attributes.IsEmpty)
					return this;
				if (IsEmpty)
					return attributes;
			}
			var clone = Clone();
			if (attributes == null)
				return clone;
				
			if (attributes._family != null)
				clone.Family = attributes._family;
			if (attributes._typeface != null)
				clone.Typeface = attributes._typeface;
			if (clone._typeface == null && clone.Family != null)
			{
				var fontStyle = _typeface?.FontStyle ?? FontStyle.None;
				clone.Typeface = clone.Family.Typefaces.FirstOrDefault(r => r.FontStyle == fontStyle);
			}
				
			if (attributes._size != null)
				clone.Size = attributes._size;
			if (attributes._foreground != null)
				clone.Foreground = attributes._foreground;
			if (attributes._background != null)
				clone.Background = attributes._background;
			if (attributes._underline != null)
				clone.Underline = attributes._underline;
			if (attributes._strikethrough != null)
				clone.Strikethrough = attributes._strikethrough;
			if (attributes._offset != null)
				clone.Offset = attributes._offset;
			return clone;
		}
		
		internal void ClearUnmatched(Attributes? attributes)
		{
			if (_family != attributes?.Family)
				_family = null;
			if (_typeface != attributes?._typeface)
			{
				if (_typeface != null && _typeface.Family == attributes?.Family)
				{
					_family = _typeface.Family;
				}
				_typeface = null;
			}
			if (_size != attributes?._size)
				_size = null;
			if (_foreground != attributes?._foreground)
				_foreground = null;
			if (_background != attributes?._background)
				_background = null;
			if (_underline != attributes?._underline)
				_underline = null;
			if (_strikethrough != attributes?._strikethrough)
				_strikethrough = null;
			if (_offset != attributes?._offset)
				_offset = null;
		}
		
		public override bool Equals(object? obj)
		{
			if (obj == null || obj is not Attributes other)
				return false;

			if (ReferenceEquals(this, obj))
				return true;

			return 
				_typeface == other._typeface &&
				_family == other._family &&
				_size == other._size &&
				_foreground == other._foreground &&
				_background == other._background &&
				_underline == other._underline &&
				_strikethrough == other._strikethrough &&
				_offset == other._offset;				
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
			return HashCode.Combine(Font, Foreground, Background, Underline, Strikethrough, Offset);
		}
		
		public void Apply(FormattedText formattedText)
		{
			if (Font != null)
				formattedText.Font = Font;
			if (Foreground != null)
				formattedText.ForegroundBrush = Foreground;
		}
	}
}
