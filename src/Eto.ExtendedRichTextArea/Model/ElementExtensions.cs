namespace Eto.ExtendedRichTextArea.Model;

public static class ElementExtensions
{
	public static Document? GetDocument(this IElement element)
	{
		var parent = element.Parent;
		while (parent?.Parent != null)
		{
			parent = parent.Parent;
		}
		return parent as Document;
	}

}
