using System.Collections;

using Eto.Drawing;
using Eto.ExtendedRichTextArea.Model;

using HtmlAgilityPack;

namespace Eto.ExtendedRichTextArea.UnitTest
{
	// TODO: Should we make this part of the control? 
	// Might be useful, but depends on HtmlAgilityPack.
	// I created this to simplify tests with formatting for now.
	class HtmlParser
	{
		Attributes? _currentAttributes;
		readonly Document _document;

		public HtmlParser(Document document)
		{
			_document = document;
		}

		public void ParseHtml(string html)
		{
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(html);
			_currentAttributes = _document.DefaultAttributes;
			ParseRun(_document, htmlDoc.DocumentNode);
		}

		readonly Stack<Attributes> _attributeStack = new Stack<Attributes>();

		void SetAttributes(Action<Attributes>? action = null)
		{
			_currentAttributes = _currentAttributes?.Clone() ?? new Attributes();
			action?.Invoke(_currentAttributes);
		}
		
		void SaveAttributes()
		{
			if (_currentAttributes != null)
				_attributeStack.Push(_currentAttributes);
		}
		
		void RestoreAttributes()
		{
			if (_attributeStack.Count > 0)
				_currentAttributes = _attributeStack.Pop();
		}

		void ParseRun(IList document, HtmlNode node)
		{
			foreach (var child in node.ChildNodes)
			{
				var lastAttributes = _currentAttributes;
				ParseNode(document, child);
				_currentAttributes = lastAttributes;
			}
		}

		private void ParseNode(IList container, HtmlNode node)
		{
			if (node.Name == "p")
			{
				AddParagraph<ParagraphElement>(_document, node);
			}
			else if (node.Name == "ul")
			{
				AddList(_document, ListType.Unordered, node);
			}
			else if (node.Name == "ol")
			{
				AddList(_document, ListType.Ordered, node);
			}
			else
			{
				AddParagraph<ParagraphElement>(container, node);
			}

		}

		private void AddList(IList container, ListType type, HtmlNode node)
		{
			var list = new ListElement();
			list.Type = type;
			container.Add(list);

			foreach (var item in node.SelectNodes("li"))
			{
				AddParagraph<ListItemElement>(list, item);
			}
		}

		private void AddParagraph<T>(IList container, HtmlNode node)
			where T : ParagraphElement, new()
		{
			var paragraph = new T();
			SaveAttributes();

			if (node.GetAttributeValue("style", null) is string style)
			{
				var parts = style.Split(';');
				foreach (var part in parts)
				{
					var kv = part.Split(':');
					if (kv.Length == 2)
					{
						var key = kv[0].Trim();
						var value = kv[1].Trim();
						switch (key)
						{
							case "font-family":
								SetAttributes(a => a.Family = new FontFamily(value));
								break;
							case "font-size":
								SetAttributes(a => a.Size = float.Parse(value));
								break;
							case "font-weight":
								SetAttributes(a => a.Typeface = a.Family?.Typefaces.FirstOrDefault(r => r.Bold));
								break;
							case "color":
								SetAttributes(a => a.Foreground = new SolidBrush(Color.Parse(value)));
								break;
						}
					}
				}
			}

			paragraph.Attributes = _currentAttributes;
			container.Add(paragraph);
			
			ParseInlines(paragraph, node);

			RestoreAttributes();

		}

		private void ParseInlines(IList container, HtmlNode node)
		{
			foreach (var child in node.ChildNodes)
			{
				if (child.Name == "b" || child.Name == "strong")
				{
					SaveAttributes();
					SetAttributes(a => a.Typeface = a.Family?.Typefaces.FirstOrDefault(r => r.Bold));
					ParseInlines(container, child);
					RestoreAttributes();
				}
				else if (child.Name == "i" || child.Name == "em")
				{
					SaveAttributes();
					SetAttributes(a => a.Typeface = a.Family?.Typefaces.FirstOrDefault(r => r.Italic));
					ParseInlines(container, child);
					RestoreAttributes();
				}
				else if (child.Name == "u")
				{
					SaveAttributes();
					SetAttributes(a => a.Underline = true);
					ParseInlines(container, child);
					RestoreAttributes();
				}
				else if (child.Name == "#text")
				{
					var text = child.InnerText.Replace("\r", "").Replace("\n", " ");
					if (!string.IsNullOrEmpty(text))
					{
						var textElement = new TextElement
						{
							Text = text,
							Attributes = _currentAttributes
						};
						container.Add(textElement);
					}
				}
				else
				{
					ParseInlines(container, child);
				}
			}
		}
	}
}