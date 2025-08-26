using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Eto.ExtendedRichTextArea.Model;

using NUnit.Framework;

namespace Eto.ExtendedRichTextArea.UnitTest
{

	public class HtmlTests : TestBase
	{

		[Test]
		public void HtmlToDocumentShouldWork()
		{
			var html = "<p>Hello <b>World</b></p><p>This is a test</p>";
			var document = new Document();
			new HtmlParser(document).ParseHtml(html);
			Assert.That(document.Length, Is.EqualTo(26));
			Assert.That(document.Text, Is.EqualTo("Hello World\nThis is a test"));
			Assert.That(document[0].Count, Is.EqualTo(2));
			Assert.That(document[1].Count, Is.EqualTo(1));

			Assert.That(document[0][0], Is.TypeOf<TextElement>());
			Assert.That(document[0][1], Is.TypeOf<TextElement>());
			Assert.That(((TextElement)document[0][0])?.Attributes?.Font?.Bold, Is.False);
			Assert.That(((TextElement)document[0][1])?.Attributes?.Font?.Bold, Is.True);
		}
	}
}