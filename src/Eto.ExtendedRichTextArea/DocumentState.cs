using Eto.Drawing;
using Eto.ExtendedRichTextArea.Model;

namespace Eto.ExtendedRichTextArea;

internal struct DocumentState
{
	public DocumentState(TextAreaDrawable textAreaDrawable, ExtendedRichTextArea textArea)
	{
		Elements = textAreaDrawable.Document.Select(e => (IBlockElement)e.Clone()).ToList();
		CaretPosition = textAreaDrawable.Caret.Index;
		Selection = textAreaDrawable.Selection?.Clone();
		ScrollPosition = textArea.ScrollPosition;
	}

	public List<IBlockElement> Elements { get; }
	public int CaretPosition { get; }
	public DocumentRange? Selection { get; }
	public Point ScrollPosition { get; }

	internal void Restore(TextAreaDrawable textAreaDrawable, ExtendedRichTextArea textArea)
	{
		textAreaDrawable.Document.BeginEdit();
		textAreaDrawable.Document.Clear();
		textAreaDrawable.Document.AddRange(Elements);
		textAreaDrawable.Caret.SetIndex(CaretPosition, false);
		textAreaDrawable.SetSelection(Selection, true);
		textAreaDrawable.Document.EndEdit();
		textArea.ScrollPosition = ScrollPosition;
	}
}
