using System;
using Eto.Forms;
using Eto.Drawing;

namespace Eto.ExtendedRichTextArea
{
	class CaretBehavior
	{
		int _caretIndex;
		UITimer _caretTimer;
		bool _caretVisible;
		RectangleF _caretBounds;
		private TextAreaDrawable _textArea;

		float Scale => _textArea.Scale;

		Document Document => _textArea.Document;

		public event EventHandler IndexChanged;

		public int Index
		{
			get => _caretIndex;
			set
			{
				if (_caretIndex != value)
				{
					_navLocation = null;
					_caretIndex = Math.Max(0, Math.Min(value, Document.Length));
					if (_caretVisible)
					{
						InvalidateCaret(_caretBounds);
					}
					_caretVisible = true;
					CalculateCaretBounds();
					InvalidateCaret();
					IndexChanged?.Invoke(_textArea, EventArgs.Empty);
					_textArea.SelectionFont = Document.GetFont(_caretIndex);
				}
			}
		}

		PointF? _navLocation;
		
		public void Navigate(DocumentNavigationMode mode)
		{
			var location = _navLocation ?? _caretBounds.Location;
			Index = Document.Navigate(_caretIndex, mode, _navLocation);
			_navLocation = location;
		}

		public RectangleF CaretBounds => _caretBounds;

		private void InvalidateCaret(RectangleF? caretBounds = null)
		{
			var bounds = caretBounds ?? _caretBounds;
			bounds.Inflate(5, 5);
			_textArea.Invalidate(Rectangle.Ceiling(bounds));// TODO: only invalidate the caret bounds
		}

		public CaretBehavior(TextAreaDrawable textArea)
		{
			_textArea = textArea;
			_textArea.GotFocus += TextArea_GotFocus;
			_textArea.Load += TextArea_Load;
			_textArea.LostFocus += TextArea_LostFocus;
		}

		private void TextArea_LostFocus(object sender, EventArgs e)
		{
			_caretTimer ??= new UITimer(OnCaretTimer) { Interval = 0.5 };
			_caretTimer.Start();
		}

		private void TextArea_Load(object sender, EventArgs e)
		{
			CalculateCaretBounds();
		}

		private void TextArea_GotFocus(object sender, EventArgs e)
		{
			_caretTimer ??= new UITimer(OnCaretTimer) { Interval = 0.5 };
			_caretTimer.Start();

		}

		private void OnCaretTimer(object sender, EventArgs e)
		{
			_caretVisible = !_caretVisible;
			InvalidateCaret();
		}

		internal void CalculateCaretBounds()
		{
			_caretBounds = _textArea.Document.CalculateCaretBounds(_caretIndex, _textArea.SelectionFont, _textArea.ParentWindow?.Screen);
		}

		internal void Paint(PaintEventArgs e)
		{
			if (_caretVisible && !_caretBounds.IsEmpty)
			{
				e.Graphics.FillRectangle(SystemColors.ControlText, _caretBounds);
			}
		}
	}
}
