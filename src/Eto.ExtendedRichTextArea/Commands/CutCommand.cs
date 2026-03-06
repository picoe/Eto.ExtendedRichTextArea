using Eto.Forms;

namespace Eto.ExtendedRichTextArea.Commands;

class CutCommand : CopyCommand
{
	public CutCommand(TextAreaDrawable textArea)
		 : base(textArea)
	{
		MenuText = Application.Instance.Localize(typeof(ExtendedRichTextArea), "Cut");
		Shortcut = Application.Instance.CommonModifier | Keys.X;
	}
	
	public override bool Enabled
	{
		get => base.Enabled && !_textArea.ReadOnly;
		set => base.Enabled = value;
	}	

	protected override void OnExecuted(EventArgs e)
	{
		if (!_textArea.HasSelection)
			return;

		// do a copy first
		base.OnExecuted(e);

		// now remove the text to make it a cut
		var start = _textArea.Selection.Start;
		_textArea.Document.RemoveAt(start, _textArea.Selection.Length);
		_textArea.Caret.SetIndex(start, false);
		_textArea.SetSelection(null, true);
	}
}
