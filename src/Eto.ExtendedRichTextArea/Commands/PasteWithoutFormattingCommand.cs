using Eto.Forms;

namespace Eto.ExtendedRichTextArea.Commands;

class PasteWithoutFormattingCommand : PasteCommand
{
	protected override bool PlainTextOnly => true;

	public PasteWithoutFormattingCommand(TextAreaDrawable textArea)
		: base(textArea)
	{
		MenuText = Application.Instance.Localize(typeof(ExtendedRichTextArea), "Paste without Formatting");
		Shortcut = Application.Instance.CommonModifier | Keys.Shift | Keys.V;
	}
}
