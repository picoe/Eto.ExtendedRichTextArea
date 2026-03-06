using Eto.Forms;

namespace Eto.ExtendedRichTextArea.Commands;

class BoldCommand : Command
{
	readonly TextAreaDrawable _textArea;
	public BoldCommand(TextAreaDrawable textArea)
	{
		_textArea = textArea;
		MenuText = Application.Instance.Localize(typeof(ExtendedRichTextArea), "Bold");
		Shortcut = Application.Instance.CommonModifier | Keys.B;
	}

	public override bool Enabled
	{
		get => base.Enabled && !_textArea.ReadOnly;
		set => base.Enabled = value;
	}

	protected override void OnExecuted(EventArgs e)
	{
		var attributes = _textArea.TextArea.SelectionAttributes;
		attributes.Bold = !(attributes.Bold ?? false);
	}
}
