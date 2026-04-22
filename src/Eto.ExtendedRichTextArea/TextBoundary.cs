namespace Eto.ExtendedRichTextArea;

static class TextBoundary
{
	public static int GetPreviousScalarStart(string text, int index)
	{
		if (string.IsNullOrEmpty(text))
			return 0;

		index = Math.Max(0, Math.Min(index, text.Length));
		if (index == 0)
			return 0;

		if (index >= 2 && char.IsLowSurrogate(text[index - 1]) && char.IsHighSurrogate(text[index - 2]))
			return index - 2;

		return index - 1;
	}

	public static int GetNextScalarEnd(string text, int index)
	{
		if (string.IsNullOrEmpty(text))
			return 0;

		index = Math.Max(0, Math.Min(index, text.Length));
		if (index >= text.Length)
			return text.Length;

		if (index + 1 < text.Length && char.IsHighSurrogate(text[index]) && char.IsLowSurrogate(text[index + 1]))
			return index + 2;

		return index + 1;
	}
}
