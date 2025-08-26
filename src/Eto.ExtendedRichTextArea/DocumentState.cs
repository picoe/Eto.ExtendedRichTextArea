using Eto.ExtendedRichTextArea.Model;

namespace Eto.ExtendedRichTextArea
{
	internal struct DocumentState
	{
		public DocumentState(TextAreaDrawable textAreaDrawable)
		{
			Elements = textAreaDrawable.Document.Select(e => (IBlockElement)e.Clone()).ToList();
			CaretPosition = textAreaDrawable.Caret.Index;
			Selection = textAreaDrawable.Selection?.Clone();
		}

		public List<IBlockElement> Elements { get; }
		public int CaretPosition { get; }
		public DocumentRange? Selection { get; }

		internal void Restore(TextAreaDrawable textAreaDrawable)
		{
			textAreaDrawable.Document.BeginEdit();
			textAreaDrawable.Document.Clear();
			textAreaDrawable.Document.AddRange(Elements);
			textAreaDrawable.Caret.Index = CaretPosition;
			textAreaDrawable.Selection = Selection;
			textAreaDrawable.Document.EndEdit();
		}
	}
}
