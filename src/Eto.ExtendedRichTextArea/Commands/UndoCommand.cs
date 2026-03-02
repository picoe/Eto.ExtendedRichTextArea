using Eto.Forms;

namespace Eto.ExtendedRichTextArea.Commands;

class UndoCommand : Command
{
	readonly TextAreaDrawable _textArea;
	public UndoCommand(TextAreaDrawable textArea)
	{
		_textArea = textArea;
		MenuText = Application.Instance.Localize(typeof(ExtendedRichTextArea), "Undo");
		Shortcut = Application.Instance.CommonModifier | Keys.Z;
		textArea.Document.Changed += Document_Changed;
	}

	private void Document_Changed(object? sender, EventArgs e)
	{
		Enabled = _textArea.CanUndo;
	}

	protected override void OnExecuted(EventArgs e)
	{
		_textArea.Undo();
	}
}
