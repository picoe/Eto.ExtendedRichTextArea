using System;
using Eto.Forms;
using Eto.Drawing;
using Eto.ExtendedRichTextArea.Model;

namespace Eto.ExtendedRichTextArea
{
	class CaretBehavior
	{
		readonly TextAreaDrawable _textArea;
		int _caretIndex;
		UITimer? _caretTimer;
		bool _caretVisible;
		RectangleF _caretBounds;
		Document Document => _textArea.Document;

		public event EventHandler? IndexChanged;

		public int Index
		{
			get => _caretIndex;
			set
			{
				if (_caretIndex == value)
					return;
					
				_navLocation = null;
				_caretIndex = Math.Max(0, Math.Min(value, Document.Length));
				if (_caretVisible)
				{
					InvalidateCaret(_caretBounds);
				}
				_caretVisible = true;
				CalculateCaretBounds();
				InvalidateCaret();
				if (_textArea.Selection == null)
					_textArea.TextArea.SelectionAttributes = Document.GetAttributes(_caretIndex, _caretIndex);
				
				IndexChanged?.Invoke(_textArea, EventArgs.Empty);
			}
		}

		PointF? _navLocation;
		
		public void Navigate(DocumentNavigationMode mode)
		{
			var location = _navLocation ?? _caretBounds.Location;

			Index = Document.Navigate(_caretIndex, mode, location);
			
			if (mode == DocumentNavigationMode.NextLine || mode == DocumentNavigationMode.PreviousLine)
				_navLocation = location;
			else
				_navLocation = null;
		}

		public RectangleF CaretBounds => _caretBounds;

		private void InvalidateCaret(RectangleF? caretBounds = null)
		{
			var bounds = caretBounds ?? _caretBounds;
			bounds.Inflate(5, 5);
			// TODO: only invalidate the caret bounds, perhaps put the caret in a child drawable?
			if (Eto.Platform.Instance.IsWpf)
				_textArea.Invalidate(false);
			else
				_textArea.Invalidate(Rectangle.Ceiling(bounds));
		}

		public CaretBehavior(TextAreaDrawable textArea)
		{
			_textArea = textArea;
			_textArea.GotFocus += TextArea_GotFocus;
			_textArea.Load += TextArea_Load;
			_textArea.LostFocus += TextArea_LostFocus;
		}

		private void TextArea_LostFocus(object? sender, EventArgs e)
		{
			_caretTimer?.Stop();
			if (_caretVisible)
			{
				_caretVisible = false;
				InvalidateCaret();
			}
		}

		private void TextArea_Load(object? sender, EventArgs e)
		{
			CalculateCaretBounds();
		}

		private void TextArea_GotFocus(object? sender, EventArgs e)
		{
			_caretTimer ??= new UITimer(OnCaretTimer) { Interval = 0.5 };
			_caretTimer.Start();
		}

		private void OnCaretTimer(object? sender, EventArgs e)
		{
			_caretVisible = !_caretVisible;
			InvalidateCaret();
		}

		internal void CalculateCaretBounds()
		{
			_caretBounds = _textArea.Document.CalculateCaretBounds(_caretIndex, _textArea.TextArea.SelectionFont, _textArea.ParentWindow?.Screen);
		}

		internal void Paint(PaintEventArgs e)
		{
			if (_caretVisible && !_caretBounds.IsEmpty)
			{
				e.Graphics.FillRectangle(_textArea.Document.DefaultAttributes.Foreground ?? new SolidBrush(SystemColors.ControlText), _caretBounds);
			}
		}
	}
}
