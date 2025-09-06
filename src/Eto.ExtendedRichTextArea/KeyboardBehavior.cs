using Eto.Forms;
using Eto.ExtendedRichTextArea.Model;

using System;
using Eto.ExtendedRichTextArea.Commands;
using System.Windows.Input;

namespace Eto.ExtendedRichTextArea
{
	class KeyboardBehavior
	{
		readonly TextAreaDrawable _textArea;
		readonly CaretBehavior _caret;
		readonly List<Command> _commands = new List<Command>();

		Document Document => _textArea.Document;

		public KeyboardBehavior(TextAreaDrawable textArea, CaretBehavior caret)
		{
			_caret = caret;
			_textArea = textArea;
			_textArea.TextInput += TextArea_TextInput;
			_textArea.KeyDown += TextArea_KeyDown;
			_textArea.KeyDown += TextArea_KeyDown_Navigation;
			if (Platform.Instance.IsMac)
			{
				_textArea.KeyDown += TextArea_KeyDown_Mac;
			}
			else
			{
				_textArea.KeyDown += TextArea_KeyDown_Generic;
			}

			AddCommand(new CutCommand(textArea), "cut");
			AddCommand(new CopyCommand(textArea), "copy");
			AddCommand(new PasteCommand(textArea), "paste");

		}

		void AddCommand(Command command, string? macPlatformCommand = null)
		{
			_commands.Add(command);
			if (_textArea.Platform.IsMac && macPlatformCommand != null)
				_textArea.MapPlatformCommand(macPlatformCommand, command);
		}


		private void TextArea_KeyDown_Navigation(object? sender, KeyEventArgs e)
		{
			var extendSelection = e.Modifiers.HasFlag(Keys.Shift);
			var lastCaretIndex = _caret.Index;
			
			if (Eto.Platform.Instance.IsMac)
				TextArea_KeyDown_Navigation_Mac(e);
			else
				TextArea_KeyDown_Navigation_Generic(e);
				
			if (!e.Handled)
			{
				switch (e.KeyData & ~Keys.Shift)
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
						if (!extendSelection && _textArea.Selection?.Length > 0)
						{
							_caret.Index = _textArea.Selection.Start;
							_textArea.Selection = null;
						}
						else
							_caret.Index = Math.Max(0, _caret.Index - 1);
						e.Handled = true;
						break;
					case Keys.Right:
						if (!extendSelection && _textArea.Selection?.Length > 0)
						{
							_caret.Index = _textArea.Selection.End;
							_textArea.Selection = null;
						}
						else
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
					case Keys.Control | Keys.Alt | Keys.Right:
					case Keys.Alt | Keys.Right:
						_caret.Navigate(DocumentNavigationMode.NextWord);
						e.Handled = true;
						break;
					case Keys.Control | Keys.Alt | Keys.Left:
					case Keys.Alt | Keys.Left:
						_caret.Navigate(DocumentNavigationMode.PreviousWord);
						e.Handled = true;
						break;
				}
			}
			if (e.Handled)
				_textArea.SetSelection(lastCaretIndex, extendSelection);
		}

		private void TextArea_TextInput(object? sender, TextInputEventArgs e)
		{
			if (e.Text.Length > 0)
			{
				if (char.IsControl(e.Text[0]))
					return;
				_textArea.TextArea.InsertText(e.Text);
				e.Cancel = true;
			}
		}

		private void TextArea_KeyDown_Generic(object? sender, KeyEventArgs e)
		{
			switch (e.KeyData)
			{
				case Keys.Control | Keys.A:
					_textArea.Selection = Document.GetRange(0, _textArea.Document.Length);
					e.Handled = true;
					break;
				case Keys.Application | Keys.Z:
					e.Handled = _textArea.Undo();
					break;
				case Keys.Application | Keys.Y:
					e.Handled = _textArea.Redo();
					break;
			}
		}

		private void TextArea_KeyDown_Navigation_Generic(KeyEventArgs e)
		{
			switch (e.KeyData & ~Keys.Shift)
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
			}
		}

		private void TextArea_KeyDown_Mac(object? sender, KeyEventArgs e)
		{
			switch (e.KeyData)
			{
				case Keys.Application | Keys.A:
					_textArea.Selection = Document.GetRange(0, _textArea.Document.Length);
					e.Handled = true;
					break;
				case Keys.Application | Keys.Z:
					e.Handled = _textArea.Undo();
					break;
				case Keys.Application | Keys.Shift | Keys.Z:
					e.Handled = _textArea.Redo();
					break;
			}
		}

		private void TextArea_KeyDown_Navigation_Mac(KeyEventArgs e)
		{
			switch (e.KeyData & ~Keys.Shift)
			{
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
			switch (e.KeyData)
			{
				case Keys.Backspace:
					if (_textArea.Selection?.Length > 0)
					{
						_textArea.Document.RemoveAt(_textArea.Selection.Start, _textArea.Selection.Length);
						var start = _textArea.Selection.Start;
						_textArea.Selection = null;
						_caret.Index = start;
					}
					else if (_caret.Index > 0)
					{
						_textArea.Document.RemoveAt(_caret.Index - 1, 1);
						_caret.Index--;
					}
					e.Handled = true;
					break;
				case Keys.Delete:
					if (_textArea.Selection?.Length > 0)
					{
						_textArea.Document.RemoveAt(_textArea.Selection.Start, _textArea.Selection.Length);
						var start = _textArea.Selection.Start;
						_textArea.Selection = null;
						_caret.Index = start;
					}
					else if (_caret.Index < _textArea.Document.Length)
					{
						_textArea.Document.RemoveAt(_caret.Index, 1);
					}
					e.Handled = true;
					break;
				case Keys.Enter:
					_textArea.TextArea.InsertText("\n");
					e.Handled = true;
					break;
				case Keys.Shift | Keys.Enter:
					_textArea.TextArea.InsertText("\x2028"); // line break
					e.Handled = true;
					break;
				case Keys.Tab:
					_textArea.TextArea.InsertText("\t"); // tab
					e.Handled = true;
					break;
			}

			if (!e.Handled)
			{
				foreach (var command in _commands)
				{
					if (command.Enabled && command.Shortcut == e.KeyData)
					{
						command.Execute();
						e.Handled = true;
						break;
					}
				}
			}


		}
	}
}
