using Eto.Forms;
using Eto.Drawing;
using System;
using Eto.ExtendedRichTextArea.Model;
using Eto.ExtendedRichTextArea.Commands;

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
		DocumentRange? _selection;
		readonly Command _cutCommand;
		readonly ExtendedRichTextArea _textArea;

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

		private void Document_Changed(object? sender, EventArgs e)
		{
			_caret.CalculateCaretBounds();
			Selection?.CalculateBounds();
			Size = Size.Ceiling(Document.Size);
#if DEBUG
			_isValid = Document.GetIsValid();
#endif
			Invalidate();
		}
		
		internal CaretBehavior Caret => _caret;
		internal KeyboardBehavior Keyboard => _keyboard;
		internal MouseBehavior Mouse => _mouse;
		internal ExtendedRichTextArea TextArea => _textArea;
		
		public TextAreaDrawable(ExtendedRichTextArea textArea) : base(false)
		{
			_textArea = textArea;
			_caret = new CaretBehavior(this);
			_keyboard = new KeyboardBehavior(this, _caret);
			_mouse = new MouseBehavior(this, _caret);
			_selection = new DocumentRange(0, 0);
			CanFocus = true;
			
			_cutCommand = new CutCommand(this);

			if (Platform.IsMac)
			{
				MapPlatformCommand("cut", _cutCommand);
			}
		}

		
		public DocumentRange? Selection
		{
			get => _selection;
			set
			{
				_selection = value;
				if (_selection != null)
				{
					_selection.Document = Document;
					_textArea.SelectionAttributes = Document.GetAttributes(_selection.Start, _selection.End);
				}
				SelectionChanged?.Invoke(this, EventArgs.Empty);
				
				Invalidate(false);
			}
		}
		
		public event EventHandler<EventArgs>? SelectionChanged;
		

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
				_parentScrollable.Scroll += parentScrollable_Scroll;
			}
		}

		protected override void OnUnLoad(EventArgs e)
		{
			base.OnUnLoad(e);
			if (_parentScrollable != null)
			{
				_parentScrollable.Scroll -= parentScrollable_Scroll;
				_parentScrollable = null;
			}
		}

		private void parentScrollable_Scroll(object? sender, ScrollEventArgs e)
		{
			Invalidate(false);
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
				try
				{
					var rect = _parentScrollable.RectangleToScreen(new RectangleF(_parentScrollable.ClientSize));
					clip.Intersect(RectangleFromScreen(rect));
				}
				catch
				{
					// Eto.Wpf currently has an issue getting client size before loaded during a drawing operation, so ignore for now.
				}
			}
			_selection?.Paint(e.Graphics);

			var screen = ParentWindow?.Screen ?? Screen.PrimaryScreen;	
			Document.ScreenScale = screen.Scale;
			Document.Paint(e.Graphics, clip);
			_caret.Paint(e);
		}


		internal float Scale => ParentWindow?.Screen.Scale ?? 1;

		public RectangleF CaretBounds => _caret.CaretBounds;
	}
}
