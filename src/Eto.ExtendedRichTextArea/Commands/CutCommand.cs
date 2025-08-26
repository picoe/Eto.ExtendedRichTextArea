using Eto.Forms;

namespace Eto.ExtendedRichTextArea.Commands
{
	class CutCommand : Command
	{
		readonly TextAreaDrawable _textArea;
		public CutCommand(TextAreaDrawable textArea)
		{
			Shortcut = Application.Instance.CommonModifier | Keys.X;
			_textArea = textArea;
			_textArea.SelectionChanged += TextArea_SelectionChanged;
		}

		private void TextArea_SelectionChanged(object? sender, EventArgs e)
		{
			Enabled = _textArea.Selection?.Length > 0;
		}

		protected override void OnExecuted(EventArgs e)
		{
			if (_textArea.Selection == null)
				return;
			using var clip = new Clipboard(); 
			_textArea.Document.BeginEdit();
			clip.Text = _textArea.Selection.Text;
			_textArea.Caret.Index = _textArea.Selection.Start;
			_textArea.Document.RemoveAt(_textArea.Selection.Start, _textArea.Selection.Length);
			_textArea.Selection = null;
			_textArea.Document.EndEdit();
		}
	}
}
