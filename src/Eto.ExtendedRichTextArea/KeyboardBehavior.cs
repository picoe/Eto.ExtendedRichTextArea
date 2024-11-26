using Eto.ExtendedRichTextArea.Model;
using Eto.Forms;

using System;

namespace Eto.ExtendedRichTextArea
{
	class KeyboardBehavior
	{
		readonly TextAreaDrawable _textArea;
		readonly CaretBehavior _caret;

		Document Document => _textArea.Document;

		public KeyboardBehavior(TextAreaDrawable textArea, CaretBehavior caret)
		{
			_caret = caret;
			_textArea = textArea;
			_textArea.TextInput += TextArea_TextInput;
			_textArea.KeyDown += TextArea_KeyDown;
			_textArea.KeyDown += TextArea_KeyDown_Navigation;
			if (Eto.Platform.Instance.IsMac)
			{
				_textArea.KeyDown += TextArea_KeyDown_Mac;
			}
			else
			{
				_textArea.KeyDown += TextArea_KeyDown_Generic;
			}
		}

		private void TextArea_KeyDown_Navigation(object? sender, KeyEventArgs e)
		{
			var extendSelection = e.Modifiers == Keys.Shift;
			if (!extendSelection && e.Modifiers != Keys.None)
				return;
			var lastCaretIndex = _caret.Index;
			switch (e.Key)
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
					_caret.Navigate(DocumentNavigationMode.BeginningOfLine);
					e.Handled = true;
					break;
				case Keys.End:
					_caret.Navigate(DocumentNavigationMode.EndOfLine);
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
					_caret.Navigate(DocumentNavigationMode.PreviousLine);
					e.Handled = true;
					break;
				case Keys.Down:
					_caret.Navigate(DocumentNavigationMode.NextLine);
					e.Handled = true;
					break;
			}
			if (e.Handled == true && extendSelection)
			{
				if (_textArea.Selection != null)
				{
					lastCaretIndex = _textArea.Selection.OriginalStart;
				}
				_textArea.Selection = new DocumentRange(lastCaretIndex, _caret.Index);
			}
			else if (e.Handled == true)
			{
				_textArea.Selection = null;
			}
		}

		private void TextArea_TextInput(object? sender, TextInputEventArgs e)
		{
			Document.InsertText(_caret.Index, e.Text, _textArea.SelectionFont, _textArea.SelectionBrush);
			_caret.Index += e.Text.Length;
			e.Cancel = true;
		}

		private void TextArea_KeyDown_Generic(object? sender, KeyEventArgs e)
		{
			switch (e.KeyData)
			{
				case Keys.Control | Keys.A:
					_textArea.Selection = new DocumentRange(0, _textArea.Document.Length);
					e.Handled = true;
					break;
				case Keys.PageUp:
					_caret.Index = 0;
					e.Handled = true;
					break;
				case Keys.PageDown:
					_caret.Index = _textArea.Document.Length;
					e.Handled = true;
					break;
				case Keys.Home:
					_caret.Navigate(DocumentNavigationMode.BeginningOfLine);
					e.Handled = true;
					break;
				case Keys.End:
					_caret.Navigate(DocumentNavigationMode.EndOfLine);
					e.Handled = true;
					break;
			}
		}

		private void TextArea_KeyDown_Mac(object? sender, KeyEventArgs e)
		{
			switch (e.KeyData)
			{
				case Keys.Application | Keys.A:
					_textArea.Selection = new DocumentRange(0, _textArea.Document.Length);
					e.Handled = true;
					break;
				case Keys.Application | Keys.Up:
					_caret.Index = 0;
					e.Handled = true;
					break;
				case Keys.Application | Keys.Down:
					_caret.Index = _textArea.Document.Length;
					e.Handled = true;
					break;
				case Keys.Application | Keys.Left:
					_caret.Navigate(DocumentNavigationMode.BeginningOfLine);
					e.Handled = true;
					break;
				case Keys.Application | Keys.Right:
					_caret.Navigate(DocumentNavigationMode.EndOfLine);
					e.Handled = true;
					break;
			}
		}

		private void TextArea_KeyDown(object? sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Keys.Backspace:
					if (_textArea.Selection != null)
					{
						_textArea.Document.RemoveAt(_textArea.Selection.Start, _textArea.Selection.Length);
						_caret.Index = _textArea.Selection.Start;
						_textArea.Selection = null;
					}
					else if (_caret.Index > 0)
					{
						_textArea.Document.RemoveAt(_caret.Index - 1, 1);
						_caret.Index--;
					}
					e.Handled = true;
					break;
				case Keys.Delete:
					if (_textArea.Selection != null)
					{
						_textArea.Document.RemoveAt(_textArea.Selection.Start, _textArea.Selection.Length);
						_caret.Index = _textArea.Selection.Start;
						_textArea.Selection = null;
					}
					else if (_caret.Index < _textArea.Document.Length)
					{
						_textArea.Document.RemoveAt(_caret.Index, 1);
					}
					e.Handled = true;
					break;
				case Keys.Enter:
					_textArea.Document.InsertText(_caret.Index, "\n", _textArea.SelectionFont);
					_caret.Index++;
					e.Handled = true;
					break;
			}
		}
	}
}
