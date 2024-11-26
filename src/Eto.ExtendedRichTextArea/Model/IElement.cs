namespace Eto.ExtendedRichTextArea.Model
{
	public interface IElement
	{
		int Start { get; set; }
		int Length { get; }
		int End { get; }
		int DocumentStart { get; }
		string Text { get; internal set; }
		IElement? Parent { get; set; }
		IElement? Split(int start);
		int RemoveAt(int start, int length);
		void Recalculate(int start);
		IEnumerable<(string text, int start)> EnumerateWords(int start, bool forward);
		IEnumerable<IInlineElement> EnumerateInlines(int start, int end);
		void MeasureIfNeeded();
	}
}
