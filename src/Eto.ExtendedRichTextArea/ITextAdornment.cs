using Eto.Drawing;
using Eto.ExtendedRichTextArea.Model;

namespace Eto.ExtendedRichTextArea;

/// <summary>
/// A decoration painted over the document after the text and before the caret.
/// Adornments draw on top of the rendered text (squiggles, find-highlights, etc.) without
/// participating in the document model or its persisted formatting.
/// </summary>
/// <remarks>
/// <see cref="Paint"/> is called during the control's paint pass with the same clip rectangle
/// used to render the text, so an adornment can (and should) limit its work to the visible region.
/// </remarks>
public interface ITextAdornment
{
	/// <summary>
	/// Paints the adornment. <paramref name="clipBounds"/> is the visible region in document
	/// coordinates; only content intersecting it needs to be drawn.
	/// </summary>
	void Paint(Document document, Graphics graphics, RectangleF clipBounds);
}
