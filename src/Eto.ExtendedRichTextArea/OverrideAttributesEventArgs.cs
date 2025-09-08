using Eto.ExtendedRichTextArea.Model;

namespace Eto.ExtendedRichTextArea;

public class OverrideAttributesEventArgs : EventArgs
{
	public Line Line { get; }
	public Chunk Chunk { get; }
	public Attributes Attributes { get; }

	public int Start => Chunk.Start;
	public int End => Chunk.End;
	public int Length => Chunk.Length;
	public string Text => Chunk.Element.Text;

	List<AttributeRange>? _newAttributes;
	public List<AttributeRange> NewAttributes => _newAttributes ??= new List<AttributeRange>();

	public OverrideAttributesEventArgs(Line line, Chunk chunk, Attributes attributes)
	{
		Line = line;
		Chunk = chunk;
		Attributes = attributes;
	}
}