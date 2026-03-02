using Eto.Forms;

namespace Eto.ExtendedRichTextArea.Commands;

class RedoCommand : Command
{
	readonly TextAreaDrawable _textArea;
	public RedoCommand(TextAreaDrawable textArea)
	{
		_textArea = textArea;
		MenuText = Application.Instance.Localize(typeof(ExtendedRichTextArea), "Redo");
		if (Platform.Instance.IsMac)
			Shortcut = Application.Instance.CommonModifier | Keys.Shift | Keys.Z;
		else
			Shortcut = Application.Instance.CommonModifier | Keys.Y;
		textArea.Document.Changed += Document_Changed;
	}

	private void Document_Changed(object? sender, EventArgs e)
	{
		Enabled = _textArea.CanRedo;
	}

	protected override void OnExecuted(EventArgs e)
	{
		_textArea.Redo();
	}
}
