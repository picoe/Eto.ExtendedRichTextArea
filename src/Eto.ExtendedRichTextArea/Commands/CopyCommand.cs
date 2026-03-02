using Eto.Forms;

namespace Eto.ExtendedRichTextArea.Commands;

class CopyCommand : Command
{
	internal readonly TextAreaDrawable _textArea;
	public CopyCommand(TextAreaDrawable textArea)
	{
		MenuText = Application.Instance.Localize(typeof(ExtendedRichTextArea), "Copy");
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
		if (!_textArea.HasSelection)
			return;
		var clip = new Clipboard();
		foreach (var format in DocumentFormat.AllFormats)
		{
			format.WriteDataObject(_textArea.Selection, clip);
		}
	}
}