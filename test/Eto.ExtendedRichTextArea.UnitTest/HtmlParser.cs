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
		ParagraphElement? _currentParagraph;
		Attributes? _currentAttributes;
		public void ParseHtml(Document document, string html)
		{
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(html);
			_currentAttributes = document.DefaultAttributes;
			foreach (var node in htmlDoc.DocumentNode.SelectNodes("//p"))
			{
				ParseNode(document, node);
			};
			AddParagraph(document);
			
		}
		
		void SetAttributes(Action<Attributes>? action = null)
		{
			_currentAttributes = _currentAttributes?.Clone() ?? new Attributes();
			action?.Invoke(_currentAttributes);
		}

		void ParseRun(Document document, HtmlNode node)
		{
			foreach (var child in node.ChildNodes)
			{
				var lastAttributes = _currentAttributes;
				ParseNode(document, child);
				_currentAttributes = lastAttributes;
			}
		}

		private void ParseNode(Document document, HtmlNode node)
		{
			if (node.Name == "p")
			{
				AddParagraph(document);
			}

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
								SetAttributes(a => a.ForegroundBrush = new SolidBrush(Color.Parse(value)));
								break;
						}
					}
				}
			}
			
			if (node.Name == "p")
			{
				if (_currentParagraph != null)
					_currentParagraph.Attributes = _currentAttributes;
				ParseRun(document, node);
			}
			else if (node.Name == "b")
			{
				SetAttributes(a => a.Typeface = a.Family?.Typefaces.FirstOrDefault(r => r.Bold));
				ParseRun(document, node);
			}
			else if (node.HasChildNodes)
			{
				ParseRun(document, node);
			}
			else
			{
				var span = new SpanElement { Text = node.InnerText, Attributes = _currentAttributes };
				_currentParagraph?.Add(span);
			}
		}

		private void AddParagraph(Document document)
		{
			if (_currentParagraph != null)
				document.Add(_currentParagraph);
			_currentParagraph = new ParagraphElement();
		}
	}
}