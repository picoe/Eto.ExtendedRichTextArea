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
		RunElement? _currentRun;
		Attributes? _currentAttributes;
		public void ParseHtml(Document document, string html)
		{
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(html);
			_currentAttributes = document.DefaultAttributes;
			foreach (var node in htmlDoc.DocumentNode.SelectNodes("//p"))
			{
				_currentParagraph = new ParagraphElement();
				_currentRun = new RunElement();
				ParseRun(node);
				if (_currentRun?.Length > 0)
					_currentParagraph.Add(_currentRun);

				document.Add(_currentParagraph);
			};
		}
		
		void SetAttributes(Action<Attributes>? action = null)
		{
			_currentAttributes = _currentAttributes?.Clone() ?? new Attributes();
			action?.Invoke(_currentAttributes);
		}

		void ParseRun(HtmlNode node)
		{
			foreach (var child in node.ChildNodes)
			{
				var lastAttributes = _currentAttributes;
				if (child.GetAttributeValue("style", null) is string style)
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
				
				if (child.Name == "b")
				{
					SetAttributes(a => a.Typeface = a.Family?.Typefaces.FirstOrDefault(r => r.Bold));
					ParseRun(child);
				}
				else if (child.HasChildNodes)
				{
					ParseRun(child);
				}
				else
				{
					var span = new SpanElement { Text = child.InnerText, Attributes = _currentAttributes };
					_currentRun?.Add(span);
				}
				_currentAttributes = lastAttributes;
			}

		}
	}
}