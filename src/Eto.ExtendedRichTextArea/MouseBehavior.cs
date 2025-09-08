using Eto.Forms;
using Eto.Drawing;
using Eto.ExtendedRichTextArea.Model;

namespace Eto.ExtendedRichTextArea;

class MouseBehavior
{
	private readonly TextAreaDrawable _textArea;
	private readonly CaretBehavior _caret;
	private bool _isMouseDown;
	private PointF _mouseDownLocation;
	private PointF _mouseLocation;

	private int _initialIndex;
	private int _lastCaretIndex;
	private bool _isSelectingByWord;

	private (string text, int start)? _initialWord;

	public MouseBehavior(TextAreaDrawable textArea, CaretBehavior caret)
	{
		_caret = caret;
		_textArea = textArea;
		_textArea.MouseDown += TextArea_MouseDown;
		_textArea.MouseMove += TextArea_MouseMove;
		_textArea.MouseUp += TextArea_MouseUp;
		_textArea.MouseDoubleClick += TextArea_MouseDoubleClick;
	}

	private void TextArea_MouseDoubleClick(object? sender, MouseEventArgs e)
	{
		var index = _textArea.Document.GetIndexAt(e.Location);
		if (index >= 0)
		{
			var word = _textArea.Document.EnumerateWords(index, true).FirstOrDefault();
			if (word.text != null)
			{
				_initialWord = word;
				_textArea.SetSelection(_textArea.Document.GetRange(word.start, word.start + word.text.Length), true);
				_isSelectingByWord = true;
				e.Handled = true;
			}
		}
	}

	private void TextArea_MouseUp(object? sender, MouseEventArgs e)
	{
		if (_initialIndex == _caret.Index && !_isSelectingByWord)
		{
			_textArea.SetSelection(null, true);
			_textArea.TextArea.SelectionAttributes = _textArea.Document.GetAttributes(_caret.Index, _caret.Index);
		}

		_isMouseDown = false;
		_isSelectingByWord = false;
	}

	private void TextArea_MouseMove(object? sender, MouseEventArgs e)
	{
		_mouseLocation = e.Location;
		if (_isMouseDown || _isSelectingByWord)
		{
			var index = _textArea.Document.GetIndexAt(_mouseLocation);
			if (_isSelectingByWord)
			{
				// select by word!
				var forward = index > _initialIndex;
				var word = _textArea.Document.EnumerateWords(index, forward).FirstOrDefault();
				if (word.text != null)
				{
					if (_initialWord != null)
						_initialIndex = forward ? _initialWord.Value.start : _initialWord.Value.start + _initialWord.Value.text.Length;
					index = forward ? word.start + word.text.Length : word.start;
				}
			}
			_caret.SetIndex(index, false);
			_textArea.SetCaretSelection(_initialIndex, true);
		}
	}

	private void TextArea_MouseDown(object? sender, MouseEventArgs e)
	{
		_isMouseDown = true;
		_lastCaretIndex = _caret.Index;
		_isSelectingByWord = false;
		_mouseDownLocation = e.Location;
		_mouseLocation = e.Location;
		var index = _textArea.Document.GetIndexAt(_mouseLocation);
		if (index >= 0)
		{
			_caret.SetIndex(index, false);
			_initialIndex = index;

			var extendSelection = e.Buttons == MouseButtons.Primary && e.Modifiers == Keys.Shift;
			if (extendSelection)
				_initialIndex = _lastCaretIndex;
			_textArea.SetCaretSelection(_lastCaretIndex, extendSelection);
		}
	}
}
