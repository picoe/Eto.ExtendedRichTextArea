using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Eto.ExtendedRichTextArea.Model;
using Eto.Forms;

namespace Eto.ExtendedRichTextArea.UnitTest;

[TestFixture]
public class ListTests : TestBase
{

	[Test]
	public void TestParseUnorderedList()
	{
		var html = "<ul><li>Item 1</li><li>Item 2</li><li>Item 3</li></ul>";
		var document = new Document();
		new HtmlParser(document).ParseHtml(html);
		Assert.That(document.Length, Is.EqualTo(20));
		Assert.That(document.Text, Is.EqualTo("• Item 1\n• Item 2\n• Item 3"));
		Assert.That(document.Count, Is.EqualTo(1), "Should be one paragraph");
		Assert.That(document[0], Is.TypeOf<ListElement>(), "First element should be a list");
		var list = (ListElement)document[0];
		Assert.That(list.Type, Is.TypeOf<MultipleListType>(), "List type should be unordered");
		Assert.That(((MultipleListType)list.Type).Types, Has.Count.GreaterThan(0), "List type should have multiple bullet styles");
		Assert.That(((MultipleListType)list.Type).Types[0], Is.TypeOf<UnorderedListType>(), "First bullet style should be unordered");
		Assert.That(list.Count, Is.EqualTo(3), "List should have three items");
		for (int i = 0; i < list.Count; i++)
		{
			var item = list[i];
			Assert.That(item, Is.TypeOf<ListItemElement>(), $"List item {i} should be a ListItemElement");
			Assert.That(item.Count, Is.EqualTo(1), $"List item {i} should have one inline element");
			Assert.That(item[0], Is.TypeOf<TextElement>(), $"List item {i} inline element should be a TextElement");
			Assert.That(((TextElement)item[0]).Text, Is.EqualTo($"Item {i + 1}"), $"List item {i} text should be 'Item {i + 1}'");
		}

	}

	[Test]
	public void TestParseOrderedList()
	{
		var html = "<ol><li>First</li><li>Second</li><li>Third</li></ol>";
		var document = new Document();
		new HtmlParser(document).ParseHtml(html);
		Assert.That(document.Text, Is.EqualTo("1. First\n2. Second\n3. Third"));
		Assert.That(document.Length, Is.EqualTo(18));
		Assert.That(document.Count, Is.EqualTo(1), "Should be one paragraph");
		Assert.That(document[0], Is.TypeOf<ListElement>(), "First element should be a list");
		var list = (ListElement)document[0];
		Assert.That(list.Type, Is.TypeOf<MultipleListType>(), "List type should be ordered");
		Assert.That(((MultipleListType)list.Type).Types, Has.Count.GreaterThan(0), "List type should have multiple bullet styles");
		Assert.That(((MultipleListType)list.Type).Types[0], Is.TypeOf<NumericListType>(), "First bullet style should be numeric");
		Assert.That(list.Count, Is.EqualTo(3), "List should have three items");
		for (int i = 0; i < list.Count; i++)
		{
			var item = list[i];
			Assert.That(item, Is.TypeOf<ListItemElement>(), $"List item {i} should be a ListItemElement");
			Assert.That(item.Count, Is.EqualTo(1), $"List item {i} should have one inline element");
			Assert.That(item[0], Is.TypeOf<TextElement>(), $"List item {i} inline element should be a TextElement");
			Assert.That(((TextElement)item[0]).Text, Is.EqualTo(new string[] { "First", "Second", "Third" }[i]), $"List item {i} text should be correct");
		}
	}

	[TestCase("<ol><li>Item 1</li><li>Item 2</li><li>Item 3</li></ol>", 7, "1. Item 1\nItem 2\n1. Item 3", 20, 3)]
	[TestCase("<ol><li>Item 1</li><li>Item 2</li><li>Item 3</li><li>Item 4</li></ol>", 7, "1. Item 1\nItem 2\n1. Item 3\n2. Item 4", 27, 3)]
	[TestCase("<ol><li>Item 1</li><li>Item 2</li><li>Item 3</li><li>Item 4</li></ol>", 0, "Item 1\n1. Item 2\n2. Item 3\n3. Item 4", 27, 2)]
	public void TestDeleteWithinList(string html, int location, string expectedText, int expectedLength, int expectedCount)
	{
		var document = new Document();
		new HtmlParser(document).ParseHtml(html);
		Assert.That(document.IsValid(), Is.True, "Document should be valid");

		((IElement)document).OnKeyDown(location, location, new KeyEventArgs(Keys.Backspace, KeyEventType.KeyDown));

		Assert.That(document.Text, Is.EqualTo(expectedText));
		Assert.That(document.IsValid(), Is.True, "Document should be valid after backspace");
		Assert.That(document.Length, Is.EqualTo(expectedLength));
		Assert.That(document.Count, Is.EqualTo(expectedCount), "Should be three top level elements");

		// Assert.That(document[0], Is.TypeOf<ListElement>(), "First element should be a list");
		// Assert.That(((ListElement)document[0]).Type, Is.TypeOf<OrderedListType>(), "List type should be ordered");

		// Assert.That(document[1], Is.TypeOf<ParagraphElement>(), "Second element should be a ParagraphElement");

		// Assert.That(document[2], Is.TypeOf<ListElement>(), "Third element should be a list");
		// Assert.That(((ListElement)document[2]).Type, Is.TypeOf<OrderedListType>(), "List type should be ordered");

	}

	[TestCase("<ul><li>Item 1</li><li></li><li>Item 3</li></ul>", 7, "• Item 1\n\n• Item 3", 14, 3)]
	[TestCase("<ul><li>Item 1</li><li></li><li>Item 3</li><li>Item 4</li></ul>", 7, "• Item 1\n\n• Item 3\n• Item 4", 21, 3)]
	[TestCase("<ul><li></li><li>Item 2</li><li>Item 3</li><li>Item 4</li></ul>", 0, "\n• Item 2\n• Item 3\n• Item 4", 21, 2)]
	public void TestEnterWithEmptyEntry(string html, int location, string expectedText, int expectedLength, int expectedCount)
    {
		var document = new Document();
		new HtmlParser(document).ParseHtml(html);
		Assert.That(document.IsValid(), Is.True, "Document should be valid");
		document.InsertText(location, "\n");

		Assert.That(document.Text, Is.EqualTo(expectedText));
		Assert.That(document.IsValid(), Is.True, "Document should be valid after enter");
		Assert.That(document.Length, Is.EqualTo(expectedLength));
		Assert.That(document.Count, Is.EqualTo(expectedCount), $"Should be {expectedCount} top level elements");
		
    }
}