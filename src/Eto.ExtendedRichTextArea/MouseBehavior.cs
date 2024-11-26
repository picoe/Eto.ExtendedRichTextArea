using Eto.Forms;
using Eto.Drawing;
using Eto.ExtendedRichTextArea.Model;

namespace Eto.ExtendedRichTextArea
{
    class MouseBehavior
	{
		private readonly TextAreaDrawable _textArea;
		private readonly CaretBehavior _caret;
		private bool _isMouseDown;
		private PointF _mouseDownLocation;
		private PointF _mouseLocation;

		private int _initialIndex;
		
		public MouseBehavior(TextAreaDrawable textArea, CaretBehavior caret)
		{
			_caret = caret;
			_textArea = textArea;
			_textArea.MouseDown += TextArea_MouseDown;
			_textArea.MouseMove += TextArea_MouseMove;
			_textArea.MouseUp += TextArea_MouseUp;
		}

		private void TextArea_MouseUp(object? sender, MouseEventArgs e)
		{
			_isMouseDown = false;
			if (_initialIndex == _caret.Index)
			{
				_textArea.Selection = null;
			}
		}

		private void TextArea_MouseMove(object? sender, MouseEventArgs e)
		{
			_mouseLocation = e.Location;
			if (_isMouseDown)
			{
				var index = _textArea.Document.GetIndexAt(_mouseLocation);
				_caret.Index = index;
				_textArea.Selection = new DocumentRange(_initialIndex, index);
			}
		}

		private void TextArea_MouseDown(object? sender, MouseEventArgs e)
		{
			_isMouseDown = true;
			_mouseDownLocation = e.Location;
			_mouseLocation = e.Location;
			var index = _textArea.Document.GetIndexAt(_mouseLocation);
			if (index >= 0)
			{
				_caret.Index = index;
				_initialIndex = index;
			}
		}
	}
}
