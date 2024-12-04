using Eto.Forms;

namespace Eto.ExtendedRichTextArea.Commands
{
	class PasteCommand : Command
	{
		readonly TextAreaDrawable _textArea;
		public PasteCommand(TextAreaDrawable textArea)
		{
			Shortcut = Application.Instance.CommonModifier | Keys.V;
			_textArea = textArea;
		}

		protected override void OnExecuted(EventArgs e)
		{
			var clip = new Clipboard(); 
			if (!clip.ContainsText)
				return;
			if (_textArea.Selection != null)
				_textArea.Selection.Text = clip.Text;
			else
				_textArea.TextArea.InsertText(clip.Text);
		}
	}
}
