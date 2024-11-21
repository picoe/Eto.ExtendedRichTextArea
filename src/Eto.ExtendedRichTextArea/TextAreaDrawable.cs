using Eto.Forms;
using Eto.Drawing;
using System;

namespace Eto.ExtendedRichTextArea
{
	public class TextAreaDrawable : Drawable
	{
		Document _document;
		CaretBehavior _caret;
		KeyboardBehavior _keyboard;
		MouseBehavior _mouse;
		bool _isValid = true;
		Scrollable _parentScrollable;

		public event EventHandler CaretIndexChanged
		{
			add => _caret.IndexChanged += value;
			remove => _caret.IndexChanged -= value;
		}

		public Document Document
		{
			get => _document ?? (Document = new Document());
			set
			{
				if (_document != null)
				{
					_document.Changed -= Document_Changed;
				}
				_document = value ?? throw new ArgumentNullException(nameof(value));
				_document.Changed += Document_Changed;
				_caret.Index = 0;
				_caret.CalculateCaretBounds();
			}
		}

		private void Document_Changed(object sender, EventArgs e)
		{
			Size = Size.Ceiling(_document.Size);
#if DEBUG
			_isValid = _document.GetIsValid();
#endif
			Invalidate();
		}

		Font _selectionFont = SystemFonts.Default();

		public Font SelectionFont
		{
			get => _selectionFont;
			set
			{
				_selectionFont = value ?? _document.DefaultFont;
				_caret.CalculateCaretBounds();
				SelectionFontChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		public event EventHandler<EventArgs> SelectionFontChanged;

		public TextAreaDrawable(NewRichTextArea textArea) : base(false)
		{
			_caret = new CaretBehavior(this);
			_keyboard = new KeyboardBehavior(this, _caret);
			_mouse = new MouseBehavior(this, _caret);
			CanFocus = true;
		}

		protected override void OnTextInput(TextInputEventArgs e)
		{
			base.OnTextInput(e);
			_document.Insert(_caret.Index, e.Text, _selectionFont);
			_caret.Index += e.Text.Length;
			e.Cancel = true;
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			if (Parent is Scrollable scrollable)
			{
				_parentScrollable = scrollable;
				_parentScrollable.Scroll += _parentScrollable_Scroll;
			}
		}

		protected override void OnUnLoad(EventArgs e)
		{
			base.OnUnLoad(e);
			if (_parentScrollable != null)
			{
				_parentScrollable.Scroll -= _parentScrollable_Scroll;
				_parentScrollable = null;
			}
		}

		private void _parentScrollable_Scroll(object sender, ScrollEventArgs e)
		{
			//Invalidate();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			if (!_isValid)
			{
				using var invalidText = new FormattedText { Text = "INVALID", Font = SystemFonts.Bold(), ForegroundBrush = Brushes.Red };
				var size = invalidText.Measure();
				var point = new PointF(Size.Width - size.Width, 0);
				e.Graphics.DrawText(invalidText, point);
			}
			var clip = e.ClipRectangle;
			if (_parentScrollable != null && Loaded)
			{
				try
				{
					clip.Intersect(_parentScrollable.VisibleRect);
				}
				catch (Exception ex)
				{
				}
			}

			_document.Paint(e.Graphics, clip);
			_caret.Paint(e);
		}

		internal float Scale => ParentWindow?.Screen.Scale ?? 1;

		public RectangleF CaretBounds => _caret.CaretBounds;
	}
}
