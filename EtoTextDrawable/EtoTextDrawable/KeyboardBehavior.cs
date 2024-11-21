using Eto.Forms;
using System;

namespace EtoTextDrawable
{
    class KeyboardBehavior
	{
		TextAreaDrawable _textArea;
		CaretBehavior _caret;
		
		Document Document => _textArea.Document;

		public KeyboardBehavior(TextAreaDrawable textArea, CaretBehavior caret)
		{
			_caret = caret;
			_textArea = textArea;
			_textArea.KeyDown += TextArea_KeyDown;
		}

		private void TextArea_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.KeyData)
			{
				case Keys.PageUp:
					_caret.Index = 0;
					e.Handled = true;
					break;
				case Keys.PageDown:
					_caret.Index = _textArea.Document.Length;
					e.Handled = true;
					break;
				case Keys.Home:
					_caret.Index = Document.Navigate(_caret.Index, DocumentNavigationMode.BeginningOfLine);
					e.Handled = true;
					break;
				case Keys.End:
					_caret.Index = Document.Navigate(_caret.Index, DocumentNavigationMode.EndOfLine);
					e.Handled = true;
					break;
				case Keys.Left:
					_caret.Index = Math.Max(0, _caret.Index - 1);
					e.Handled = true;
					break;
				case Keys.Right:
					_caret.Index = Math.Min(_textArea.Document.Length, _caret.Index + 1);
					e.Handled = true;
					break;
				case Keys.Up:
					_caret.Index = Document.Navigate(_caret.Index, DocumentNavigationMode.PreviousLine);
					e.Handled = true;
					break;
				case Keys.Down:
					_caret.Index = Document.Navigate(_caret.Index, DocumentNavigationMode.NextLine);
					e.Handled = true;
					break;
				case Keys.Backspace:
					if (_caret.Index > 0)
					{
						_textArea.Document.Remove(_caret.Index - 1, 1);
						_caret.Index--;
					}
					e.Handled = true;
					break;
				case Keys.Delete:
					if (_caret.Index < _textArea.Document.Length)
					{
						_textArea.Document.Remove(_caret.Index, 1);
					}
					e.Handled = true;
					break;
				case Keys.Enter:
					_textArea.Document.Insert(_caret.Index, "\n", _textArea.InsertionFont);
					_caret.Index++;
					e.Handled = true;
					break;
			}
		}
	}
}
