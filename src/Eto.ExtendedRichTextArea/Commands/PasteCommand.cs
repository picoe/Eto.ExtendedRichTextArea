using Eto.Forms;

namespace Eto.ExtendedRichTextArea.Commands;

class PasteCommand : Command
{
	readonly TextAreaDrawable _textArea;
	public PasteCommand(TextAreaDrawable textArea)
	{
		MenuText = Application.Instance.Localize(typeof(ExtendedRichTextArea), "Paste");
		Shortcut = Application.Instance.CommonModifier | Keys.V;
		_textArea = textArea;
	}

	public override bool Enabled
	{
		get => base.Enabled && !_textArea.ReadOnly;
		set => base.Enabled = value;
	}

	protected override void OnExecuted(EventArgs e)
	{
		var clip = new Clipboard();
		var range = _textArea.Selection ?? _textArea.Document.GetRange(_textArea.Caret.Index, 0);

		foreach (var format in DocumentFormat.AllFormats)
		{
			if (format.ReadDataObject(range, clip))
			{
				_textArea.Caret.SetIndex(range.End, false);
				_textArea.SetSelection(null, true);
				return;
			}
		}

		if (!clip.ContainsText)
			return;

		range.Text = clip.Text;
		_textArea.Caret.SetIndex(range.End, false);
		_textArea.SetSelection(null, true);
	}
}