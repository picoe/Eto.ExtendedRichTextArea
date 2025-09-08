namespace Eto.ExtendedRichTextArea.Model;

public struct AttributeRange
{
	int? _end;
	int? _length;
	public int Start { get; set; }
	public int End
	{
		get => _end ?? Start + _length ?? 0;
		set
		{
			_end = value;
			_length = null;
		}
	}

	public int Length
	{
		get => _length ?? _end ?? Start - Start;
		set
		{
			_length = value;
			_end = null;
		}
	}
	public Attributes Attributes { get; set; }

	public AttributeRange(int start, int end, Attributes attributes)
	{
		Start = start;
		_end = end;
		Attributes = attributes;
	}
}