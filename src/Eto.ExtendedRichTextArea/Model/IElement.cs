namespace Eto.ExtendedRichTextArea.Model
{
	public interface IElement
	{
		public Attributes? Attributes { get; set; }
		/// <summary>
		/// The start index, relative to the parent element.
		/// E.g. the first element in a container always has a start of 0.
		/// </summary>
		int Start { get; set; }
		/// <summary>
		/// Gets the length of the element.
		/// </summary>
		int Length { get; }
		/// <summary>
		/// Gets the end index relative to the parent element.
		/// </summary>
		int End { get; }
		/// <summary>
		/// Gets the start index relative to the document.
		/// </summary>
		int DocumentStart { get; }
		/// <summary>
		/// Gets the text content of this element
		/// </summary>
		string Text { get; internal set; }
		/// <summary>
		/// Gets or sets the parent element of this element.
		/// </summary>
		IElement? Parent { get; set; }
		/// <summary>
		/// Splits the element at the specified index, returning the new element.
		/// </summary>
		/// <remarks>
		/// The new element isn't added to the parent automatically, so you must do that after calling Split()
		/// </remarks>
		/// <param name="start">The index within this element to split at</param>
		/// <returns>A new element containing the right portion after the split</returns>
		IElement? Split(int start);
		int RemoveAt(int start, int length);
		IEnumerable<(string text, int start)> EnumerateWords(int start, bool forward);
		IEnumerable<IInlineElement> EnumerateInlines(int start, int end, bool trim);
		IEnumerable<IElement> Enumerate(int start, int end, bool trimInlines);
		void MeasureIfNeeded();
	}
}
