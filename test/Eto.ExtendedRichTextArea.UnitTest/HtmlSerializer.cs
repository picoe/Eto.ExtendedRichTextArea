using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Eto.Drawing;
using Eto.ExtendedRichTextArea.Model;
using HtmlAgilityPack;


namespace Eto.ExtendedRichTextArea.UnitTest
{
    public class HtmlSerializer
    {
		public static string Serialize(Document document)
		{
			var html = new HtmlDocument();
			var root = html.CreateElement("html");
			html.DocumentNode.AppendChild(root);

			var head = html.CreateElement("head");
			root.AppendChild(head);

			var style = html.CreateElement("style");
			style.InnerHtml = "body { font-family: sans-serif; font-size: 13pt; }";
			head.AppendChild(style);

			var body = html.CreateElement("body");
			root.AppendChild(body);

			SerializeElement(html, body, document);

			return html.DocumentNode.OuterHtml;
		}

		private static void SerializeElement(HtmlDocument html, HtmlNode parent, IElement element)
		{
			switch (element)
			{
				case TextElement span:
					var spanNode = html.CreateElement("span");
					spanNode.InnerHtml = span.Text;
					parent.AppendChild(spanNode);
					break;
				// case LineBreakElement _:
				// 	parent.AppendChild(html.CreateElement("br"));
				// 	break;
				case ImageElement image:
					var imgNode = html.CreateElement("img");
					if (image.Image is Bitmap bmp)
					{
						var bytes = bmp.ToByteArray(ImageFormat.Png);
						var base64 = Convert.ToBase64String(bytes);
						imgNode.SetAttributeValue("src", $"data:image/png;base64,{base64}");
					}
					// imgNode.SetAttributeValue("src", image.ImagePath);
					// imgNode.SetAttributeValue("alt", image.AltText);
					parent.AppendChild(imgNode);
					break;
				case ParagraphElement container:
					var divNode = html.CreateElement("p");
					parent.AppendChild(divNode);
					foreach (var child in container)
					{
						SerializeElement(html, divNode, child);
					}
					break;
			}
		}
    }
}