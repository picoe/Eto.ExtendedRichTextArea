using Eto.Forms;
using Eto.Drawing;

namespace Eto.ExtendedRichTextArea
{
    class MouseBehavior
	{
		private TextAreaDrawable _textArea;
		private CaretBehavior _caret;
		private bool _isMouseDown;
		private PointF _mouseDownLocation;
		private PointF _mouseLocation;
		
		public MouseBehavior(TextAreaDrawable textArea, CaretBehavior caret)
		{
			_caret = caret;
			_textArea = textArea;
			_textArea.MouseDown += TextArea_MouseDown;
			_textArea.MouseMove += TextArea_MouseMove;
			_textArea.MouseUp += TextArea_MouseUp;
		}

		private void TextArea_MouseUp(object sender, MouseEventArgs e)
		{
			_isMouseDown = false;
		}

		private void TextArea_MouseMove(object sender, MouseEventArgs e)
		{
			_mouseLocation = e.Location;
			if (_isMouseDown)
			{
				var index = _textArea.Document.GetIndexAtPoint(_mouseLocation);
				_caret.Index = index;
			}
		}

		private void TextArea_MouseDown(object sender, MouseEventArgs e)
		{
			_isMouseDown = true;
			_mouseDownLocation = e.Location;
			_mouseLocation = e.Location;
			var index = _textArea.Document.GetIndexAtPoint(_mouseLocation);
			if (index > 0)
				_caret.Index = index;
		}
	}
}
