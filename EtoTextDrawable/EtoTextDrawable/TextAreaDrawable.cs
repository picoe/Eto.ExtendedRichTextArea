using Eto.Forms;
using Eto.Drawing;
using System;

namespace EtoTextDrawable
{

    public class TextAreaDrawable : Drawable
	{
		Document _document;
		CaretBehavior _caret;
		KeyboardBehavior _keyboard;
		bool _isValid = true;

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

        Font _insertionFont = SystemFonts.Default();

		public Font InsertionFont
		{
			get => _insertionFont;
			set
			{
				_insertionFont = value ?? _document.DefaultFont;
				_caret.CalculateCaretBounds();
				InsertionFontChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		public event EventHandler<EventArgs> InsertionFontChanged;

		public TextAreaDrawable(NewRichTextArea textArea)
		{
			_caret = new CaretBehavior(this);
			_keyboard = new KeyboardBehavior(this, _caret);
			CanFocus = true;
		}

		protected override void OnTextInput(TextInputEventArgs e)
		{
			base.OnTextInput(e);
			_document.Insert(_caret.Index, e.Text, _insertionFont);
			_caret.Index += e.Text.Length;
			e.Cancel = true;
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

			_document.Paint(e.Graphics);
			_caret.Paint(e);
		}

		internal float Scale => ParentWindow?.Screen.Scale ?? 1;

        public RectangleF CaretBounds => _caret.CaretBounds;
    }
}
