using Eto.Forms;

namespace Eto.ExtendedRichTextArea.Commands;

class ItalicCommand : Command
{
	readonly TextAreaDrawable _textArea;
	public ItalicCommand(TextAreaDrawable textArea)
	{
		_textArea = textArea;
		MenuText = Application.Instance.Localize(typeof(ExtendedRichTextArea), "Italic");
		Shortcut = Application.Instance.CommonModifier | Keys.I;
	}

	public override bool Enabled
	{
		get => base.Enabled && !_textArea.ReadOnly;
		set => base.Enabled = value;
	}

	protected override void OnExecuted(EventArgs e)
	{
		var attributes = _textArea.TextArea.SelectionAttributes;
		attributes.Italic = !(attributes.Italic ?? false);
	}
}
