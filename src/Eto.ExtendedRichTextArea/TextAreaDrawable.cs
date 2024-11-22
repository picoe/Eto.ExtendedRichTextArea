using Eto.Forms;
using Eto.Drawing;
using System;
using Eto.ExtendedRichTextArea.Model;

namespace Eto.ExtendedRichTextArea
{
	class TextAreaDrawable : Drawable
	{
		Document? _document;
		readonly CaretBehavior _caret;
		readonly KeyboardBehavior _keyboard;
		readonly MouseBehavior _mouse;
		bool _isValid = true;
		Scrollable? _parentScrollable;

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
			Size = Size.Ceiling(Document.Size);
#if DEBUG
			_isValid = Document.GetIsValid();
#endif
			Invalidate();
		}

		Font? _selectionFont;

		public Font SelectionFont
		{
			get => _selectionFont ?? Document.DefaultFont;
			set
			{
				_selectionFont = value;
				_caret.CalculateCaretBounds();
				SelectionFontChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		public event EventHandler<EventArgs>? SelectionFontChanged;

		Brush? _selectionBrush;

		public Brush SelectionBrush
		{
			get => _selectionBrush ?? Document.DefaultBrush;
			set
			{
				_selectionBrush = value;
				SelectionBrushChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		public event EventHandler<EventArgs>? SelectionBrushChanged;

		public TextAreaDrawable(ExtendedRichTextArea textArea) : base(false)
		{
			_caret = new CaretBehavior(this);
			_keyboard = new KeyboardBehavior(this, _caret);
			_mouse = new MouseBehavior(this, _caret);
			CanFocus = true;
		}
		
		DocumentRange? _selection;
		
		public DocumentRange? Selection
		{
			get => _selection;
			set
			{
				_selection = value;
				if (_selection != null)
				{
					_selection.Document = Document;
					_selection.CalculateBounds();
				}
				Invalidate(false);
			}
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

#if DEBUG
			if (!_isValid)
			{
				using var invalidText = new FormattedText { Text = "INVALID", Font = SystemFonts.Bold(), ForegroundBrush = Brushes.Red };
				var size = invalidText.Measure();
				var point = new PointF(Size.Width - size.Width, 0);
				e.Graphics.DrawText(invalidText, point);
			}
#endif
			var clip = e.ClipRectangle;
			if (_parentScrollable != null && Loaded)
			{
				var rect = _parentScrollable.RectangleToScreen(new RectangleF(_parentScrollable.ClientSize));
				clip.Intersect(RectangleFromScreen(rect));
			}
			_selection?.Paint(e.Graphics);

			Document.Paint(e.Graphics, clip);
			_caret.Paint(e);
		}

		internal float Scale => ParentWindow?.Screen.Scale ?? 1;

		public RectangleF CaretBounds => _caret.CaretBounds;
	}
}
