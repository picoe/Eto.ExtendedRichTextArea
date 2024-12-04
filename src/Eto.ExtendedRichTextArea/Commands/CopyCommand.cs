using Eto.Forms;

namespace Eto.ExtendedRichTextArea.Commands
{
	class CopyCommand : Command
	{
		readonly TextAreaDrawable _textArea;
		public CopyCommand(TextAreaDrawable textArea)
		{
			Shortcut = Application.Instance.CommonModifier | Keys.C;
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
			var clip = new Clipboard(); 
			clip.Text = _textArea.Selection.Text;
		}
	}
}
