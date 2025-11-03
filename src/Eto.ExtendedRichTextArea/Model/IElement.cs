using Eto.Forms;

namespace Eto.ExtendedRichTextArea.Model;

public interface IElement : ICloneable
{
	/// <summary>
	/// Gets or sets the attributes for this element.
	/// </summary>
	public Attributes? Attributes { get; set; }	
	/// <summary>
	/// Gets the resolved attributes for this element, taking into account parent attributes and default attributes.
	/// </summary>
	public Attributes ActualAttributes { get; }
	/// <summary>
	/// The start index, relative to the parent element.
	/// E.g. the first element in a container always has a start of 0.
	/// </summary>
	int Start { get; internal set; }
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
	IBlockElement? Parent { get; set; }
	/// <summary>
	/// Splits the element at the specified index, returning the new element.
	/// </summary>
	/// <remarks>
	/// The new element isn't added to the parent automatically, so you must do that after calling Split()
	/// </remarks>
	/// <param name="start">The index within this element to split at</param>
	/// <returns>A new element containing the right portion after the split</returns>
	IElement? Split(int start);
	bool InsertAt(int start, IElement element);
	int RemoveAt(int start, int length);
	IEnumerable<(string text, int start)> EnumerateWords(int start, bool forward);
	IEnumerable<IInlineElement> EnumerateInlines(int start, int end, bool trim);
	IEnumerable<IElement> Enumerate(int start, int end, bool trimInlines);
	void MeasureIfNeeded();
	void OnKeyDown(int start, int end, KeyEventArgs args);
}
