using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Eto.ExtendedRichTextArea.Model;

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
		Assert.That(list.Type, Is.TypeOf<UnorderedListType>(), "List type should be unordered");
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
		Assert.That(list.Type, Is.TypeOf<OrderedListType>(), "List type should be ordered");
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

}