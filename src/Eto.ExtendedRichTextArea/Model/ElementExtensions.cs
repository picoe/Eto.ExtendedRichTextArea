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
	
	public static T? GetParentOfType<T>(this IElement element) where T : class, IElement
	{
		var parent = element?.Parent;
		while (parent != null)
		{
			if (parent is T t)
				return t;
			parent = parent.Parent;
		}
		return null;
	}

}
