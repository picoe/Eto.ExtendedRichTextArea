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
	public void ToggleLastListItemShouldKeepParagraphBelow()
	{
		// List followed by a blank paragraph
		var html = "<ul><li>Item 1</li><li></li></ul><p>after</p>";
		var document = new Document();
		Assert.That(DocumentFormat.Html.LoadFromString(document.DocumentRange, html), Is.True);
		Assert.That(document.IsValid(), Is.True);
		Assert.That(document.Count, Is.EqualTo(2), "Should start with list and blank paragraph");

		// Toggle the last item in the list off (to paragraph)
		var list = (ListElement)document[0];
		var lastItem = list[list.Count - 1];
		var lastItemStart = lastItem.DocumentStart;
		var lastItemEnd = lastItemStart + lastItem.Length;
		var range = document.GetRange(lastItemStart, lastItemEnd);
		range.ReplaceWithList(ListType.Unordered); // same type → toggle off

		Assert.That(document.IsValid(), Is.True, "Document should be valid after toggle");
		Assert.That(document.Count, Is.EqualTo(3), "Should have list, paragraph, blank paragraph");
		Assert.That(document[0], Is.TypeOf<ListElement>(), "First element should be a list");
		Assert.That(document[1], Is.TypeOf<ParagraphElement>(), "Second element should be a paragraph (was last list item)");
		Assert.That(document[2], Is.TypeOf<ParagraphElement>(), "Third element should be the blank paragraph");
		var firstList = (ListElement)document[0];
		Assert.That(firstList.Count, Is.EqualTo(1), "First list should have one item");
		Assert.That(((TextElement)firstList[0][0]).Text, Is.EqualTo("Item 1"));
		var secondPara = (ParagraphElement)document[1];
		Assert.That(secondPara.Count, Is.EqualTo(0), "Second paragraph should have no inline elements");
		Assert.That(secondPara.Text, Is.EqualTo(""), "Second paragraph should be empty");
		var thirdPara = (ParagraphElement)document[2];
		Assert.That(thirdPara.Count, Is.EqualTo(1), "Blank paragraph should have one inline element");
		Assert.That(thirdPara.Text, Is.EqualTo("after"), "Blank paragraph should contain 'after'");
	}

	[Test]
	public void TestParseUnorderedList()
	{
		var html = "<ul><li>Item 1</li><li>Item 2</li><li>Item 3</li></ul>";
		var document = new Document();
		var loaded = DocumentFormat.Html.LoadFromString(document.DocumentRange, html);
		Assert.That(loaded, Is.True);
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
		var loaded = DocumentFormat.Html.LoadFromString(document.DocumentRange, html);
		Assert.That(loaded, Is.True);
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
		var loaded = DocumentFormat.Html.LoadFromString(document.DocumentRange, html);
		Assert.That(loaded, Is.True);
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
		var loaded = DocumentFormat.Html.LoadFromString(document.DocumentRange, html);
		Assert.That(loaded, Is.True);
		Assert.That(document.IsValid(), Is.True, "Document should be valid");
		document.InsertText(location, "\n");

		Assert.That(document.Text, Is.EqualTo(expectedText));
		Assert.That(document.IsValid(), Is.True, "Document should be valid after enter");
		Assert.That(document.Length, Is.EqualTo(expectedLength));
		Assert.That(document.Count, Is.EqualTo(expectedCount), $"Should be {expectedCount} top level elements");
		
    }

	[Test]
	public void ReplaceSecondListItemWithOrderedListShouldWork()
	{
		// Create an unordered (bulleted) list with three items
		var html = "<ul><li>Item 1</li><li>Item 2</li><li>Item 3</li></ul>";
		var document = new Document();
		var loaded = DocumentFormat.Html.LoadFromString(document.DocumentRange, html);
		Assert.That(loaded, Is.True);
		Assert.That(document.IsValid(), Is.True, "Document should be valid after load");
		Assert.That(document.Count, Is.EqualTo(1), "Should start with one list block");
		Assert.That(document.Length, Is.EqualTo(20));

		var originalList = (ListElement)document[0];
		Assert.That(originalList.Count, Is.EqualTo(3), "List should have three items");

		// Get the range of the second list item using DocumentStart
		var item2 = originalList[1];
		var item2Start = item2.DocumentStart;
		var item2End = item2Start + item2.Length;
		var range = document.GetRange(item2Start, item2End);

		// Replace the second item with a numeric (ordered) list
		range.ReplaceWithList(ListType.Ordered);

		Assert.That(document.IsValid(), Is.True, "Document should be valid after replace");
		Assert.That(document.Count, Is.EqualTo(3), "Document should have three top-level list blocks after replace");
		Assert.That(document.Length, Is.EqualTo(20), "Total length should remain unchanged");

		// First block: unordered list with "Item 1"
		Assert.That(document[0], Is.TypeOf<ListElement>(), "First block should be a list");
		var firstList = (ListElement)document[0];
		Assert.That(firstList.Type, Is.TypeOf<MultipleListType>(), "First list type should be MultipleListType");
		Assert.That(((MultipleListType)firstList.Type).Types[0], Is.TypeOf<UnorderedListType>(), "First list should be unordered");
		Assert.That(firstList.Count, Is.EqualTo(1), "First list should have one item");
		Assert.That(((TextElement)firstList[0][0]).Text, Is.EqualTo("Item 1"), "First list item text should be 'Item 1'");

		// Second block: ordered (numeric) list with "Item 2"
		Assert.That(document[1], Is.TypeOf<ListElement>(), "Second block should be a list");
		var secondList = (ListElement)document[1];
		Assert.That(secondList.Type, Is.TypeOf<MultipleListType>(), "Second list type should be MultipleListType");
		Assert.That(((MultipleListType)secondList.Type).Types[0], Is.TypeOf<NumericListType>(), "Second list should be numeric/ordered");
		Assert.That(secondList.Count, Is.EqualTo(1), "Numeric list should have one item");
		Assert.That(((TextElement)secondList[0][0]).Text, Is.EqualTo("Item 2"), "Second list item text should be 'Item 2'");

		// Third block: unordered list with "Item 3"
		Assert.That(document[2], Is.TypeOf<ListElement>(), "Third block should be a list");
		var thirdList = (ListElement)document[2];
		Assert.That(thirdList.Type, Is.TypeOf<MultipleListType>(), "Third list type should be MultipleListType");
		Assert.That(((MultipleListType)thirdList.Type).Types[0], Is.TypeOf<UnorderedListType>(), "Third list should be unordered");
		Assert.That(thirdList.Count, Is.EqualTo(1), "Third list should have one item");
		Assert.That(((TextElement)thirdList[0][0]).Text, Is.EqualTo("Item 3"), "Third list item text should be 'Item 3'");
	}

	[Test]
	public void ReplaceWithShouldMergeRightAdjacentListOfSameType()
	{
		// Bullet list followed by a numeric list.
		// Converting the tail of the bullet list to numeric should merge with the numeric list.
		var html = "<ul><li>A</li><li>B</li><li>C</li></ul><ol><li>D</li><li>E</li></ol>";
		var document = new Document();
		Assert.That(DocumentFormat.Html.LoadFromString(document.DocumentRange, html), Is.True);
		Assert.That(document.IsValid(), Is.True);
		Assert.That(document.Count, Is.EqualTo(2), "Should start with two lists");

		// Select "B" and "C" from the bullet list (last two items).
		var bulletList = (ListElement)document[0];
		var itemB = bulletList[1];
		var itemC = bulletList[2];
		var range = document.GetRange(itemB.DocumentStart, itemC.DocumentStart + itemC.Length);

		range.ReplaceWithList(ListType.Ordered);

		Assert.That(document.IsValid(), Is.True, "Document should be valid after replace");
		Assert.That(document.Count, Is.EqualTo(2), "Should have two lists after merge");

		// First list: bullet with just "A"
		var firstList = (ListElement)document[0];
		Assert.That(((MultipleListType)firstList.Type).Types[0], Is.TypeOf<UnorderedListType>(), "First list should be unordered");
		Assert.That(firstList.Count, Is.EqualTo(1));
		Assert.That(((TextElement)firstList[0][0]).Text, Is.EqualTo("A"));

		// Second list: numeric with B, C, D, E (merged)
		var secondList = (ListElement)document[1];
		Assert.That(((MultipleListType)secondList.Type).Types[0], Is.TypeOf<NumericListType>(), "Second list should be numeric");
		Assert.That(secondList.Count, Is.EqualTo(4), "Merged list should have 4 items");
		Assert.That(((TextElement)secondList[0][0]).Text, Is.EqualTo("B"));
		Assert.That(((TextElement)secondList[1][0]).Text, Is.EqualTo("C"));
		Assert.That(((TextElement)secondList[2][0]).Text, Is.EqualTo("D"));
		Assert.That(((TextElement)secondList[3][0]).Text, Is.EqualTo("E"));
	}

	[Test]
	public void ReplaceWithShouldMergeLeftAdjacentListOfSameType()
	{
		// Numeric list followed by a bullet list.
		// Converting the head of the bullet list to numeric should merge with the numeric list.
		var html = "<ol><li>A</li><li>B</li></ol><ul><li>C</li><li>D</li><li>E</li></ul>";
		var document = new Document();
		Assert.That(DocumentFormat.Html.LoadFromString(document.DocumentRange, html), Is.True);
		Assert.That(document.IsValid(), Is.True);
		Assert.That(document.Count, Is.EqualTo(2), "Should start with two lists");

		// Select "C" and "D" from the bullet list (first two items).
		var bulletList = (ListElement)document[1];
		var itemC = bulletList[0];
		var itemD = bulletList[1];
		var range = document.GetRange(itemC.DocumentStart, itemD.DocumentStart + itemD.Length);

		range.ReplaceWithList(ListType.Ordered);

		Assert.That(document.IsValid(), Is.True, "Document should be valid after replace");
		Assert.That(document.Count, Is.EqualTo(2), "Should have two lists after merge");

		// First list: numeric with A, B, C, D (merged)
		var firstList = (ListElement)document[0];
		Assert.That(((MultipleListType)firstList.Type).Types[0], Is.TypeOf<NumericListType>(), "First list should be numeric");
		Assert.That(firstList.Count, Is.EqualTo(4), "Merged list should have 4 items");
		Assert.That(((TextElement)firstList[0][0]).Text, Is.EqualTo("A"));
		Assert.That(((TextElement)firstList[1][0]).Text, Is.EqualTo("B"));
		Assert.That(((TextElement)firstList[2][0]).Text, Is.EqualTo("C"));
		Assert.That(((TextElement)firstList[3][0]).Text, Is.EqualTo("D"));

		// Second list: bullet with just "E"
		var secondList = (ListElement)document[1];
		Assert.That(((MultipleListType)secondList.Type).Types[0], Is.TypeOf<UnorderedListType>(), "Second list should be unordered");
		Assert.That(secondList.Count, Is.EqualTo(1));
		Assert.That(((TextElement)secondList[0][0]).Text, Is.EqualTo("E"));
	}

	[Test]
	public void ReplaceWithShouldMergeBothAdjacentListsOfSameType()
	{
		// Numeric list, then bullet list, then numeric list.
		// Converting the entire bullet list to numeric should merge all three into one.
		var html = "<ol><li>A</li><li>B</li></ol><ul><li>C</li><li>D</li></ul><ol><li>E</li><li>F</li></ol>";
		var document = new Document();
		Assert.That(DocumentFormat.Html.LoadFromString(document.DocumentRange, html), Is.True);
		Assert.That(document.IsValid(), Is.True);
		Assert.That(document.Count, Is.EqualTo(3), "Should start with three lists");

		// Select all items in the bullet list.
		var bulletList = (ListElement)document[1];
		var range = document.GetRange(bulletList.DocumentStart, bulletList.DocumentStart + bulletList.Length);

		range.ReplaceWithList(ListType.Ordered);

		Assert.That(document.IsValid(), Is.True, "Document should be valid after replace");
		Assert.That(document.Count, Is.EqualTo(1), "All three lists should merge into one");

		var merged = (ListElement)document[0];
		Assert.That(((MultipleListType)merged.Type).Types[0], Is.TypeOf<NumericListType>(), "Merged list should be numeric");
		Assert.That(merged.Count, Is.EqualTo(6), "Merged list should have all 6 items");
		var expectedTexts = new[] { "A", "B", "C", "D", "E", "F" };
		for (int i = 0; i < 6; i++)
			Assert.That(((TextElement)merged[i][0]).Text, Is.EqualTo(expectedTexts[i]), $"Item {i} text should be '{expectedTexts[i]}'");
	}

	[Test]
	public void ReplaceWithSpanningBulletNumericBulletShouldProduceCorrectLists()
	{
		// bullet(A,B,C) + numeric(D,E) + bullet(F,G,H)
		// Select B,C from first bullet, all of numeric, and F,G from last bullet → convert to ordered.
		// Expected result: bullet(A) | ordered(B,C,D,E,F,G) | bullet(H)
		var html = "<ul><li>A</li><li>B</li><li>C</li></ul><ol><li>D</li><li>E</li></ol><ul><li>F</li><li>G</li><li>H</li></ul>";
		var document = new Document();
		Assert.That(DocumentFormat.Html.LoadFromString(document.DocumentRange, html), Is.True);
		Assert.That(document.IsValid(), Is.True);
		Assert.That(document.Count, Is.EqualTo(3), "Should start with three lists");

		var bullet1 = (ListElement)document[0];
		var bullet3 = (ListElement)document[2];

		// Range: from "B" (index 1 of bullet1) through "G" (index 1 of bullet3)
		var itemB = bullet1[1];
		var itemG = bullet3[1];
		var range = document.GetRange(itemB.DocumentStart, itemG.DocumentStart + itemG.Length);

		range.ReplaceWithList(ListType.Ordered);

		Assert.That(document.IsValid(), Is.True, "Document should be valid after replace");
		Assert.That(document.Count, Is.EqualTo(3), "Should have three lists: bullet(A), ordered(B-G), bullet(H)");

		// First block: bullet list with just "A"
		var firstList = (ListElement)document[0];
		Assert.That(((MultipleListType)firstList.Type).Types[0], Is.TypeOf<UnorderedListType>(), "First list should be unordered");
		Assert.That(firstList.Count, Is.EqualTo(1), "First list should have 1 item");
		Assert.That(((TextElement)firstList[0][0]).Text, Is.EqualTo("A"));

		// Second block: ordered list with B, C, D, E, F, G
		var secondList = (ListElement)document[1];
		Assert.That(((MultipleListType)secondList.Type).Types[0], Is.TypeOf<NumericListType>(), "Second list should be numeric/ordered");
		Assert.That(secondList.Count, Is.EqualTo(6), "Ordered list should have 6 items: B,C,D,E,F,G");
		var expectedOrdered = new[] { "B", "C", "D", "E", "F", "G" };
		for (int i = 0; i < 6; i++)
			Assert.That(((TextElement)secondList[i][0]).Text, Is.EqualTo(expectedOrdered[i]), $"Ordered item {i} should be '{expectedOrdered[i]}'");

		// Third block: bullet list with just "H"
		var thirdList = (ListElement)document[2];
		Assert.That(((MultipleListType)thirdList.Type).Types[0], Is.TypeOf<UnorderedListType>(), "Third list should be unordered");
		Assert.That(thirdList.Count, Is.EqualTo(1), "Third list should have 1 item");
		Assert.That(((TextElement)thirdList[0][0]).Text, Is.EqualTo("H"));
	}

	// ── toggle-off tests ─────────────────────────────────────────────────────

	[Test]
	public void ToggleOffWholeSingleListShouldProduceParagraphs()
	{
		// An unordered list with three items; selecting all items and calling
		// ReplaceWithList with the same type should turn every item into a paragraph.
		var html = "<ul><li>A</li><li>B</li><li>C</li></ul>";
		var document = new Document();
		Assert.That(DocumentFormat.Html.LoadFromString(document.DocumentRange, html), Is.True);
		Assert.That(document.IsValid(), Is.True);
		Assert.That(document.Count, Is.EqualTo(1));

		var list = (ListElement)document[0];
		var range = document.GetRange(list.DocumentStart, list.DocumentStart + list.Length);
		range.ReplaceWithList(ListType.Unordered);

		Assert.That(document.IsValid(), Is.True, "Document should be valid after toggle-off");
		Assert.That(document.Count, Is.EqualTo(3), "Should have three plain paragraphs");
		for (int i = 0; i < 3; i++)
		{
			Assert.That(document[i], Is.TypeOf<ParagraphElement>(), $"Element {i} should be a ParagraphElement");
			Assert.That(document[i], Is.Not.TypeOf<ListItemElement>(), $"Element {i} must not be a ListItemElement");
			Assert.That(((TextElement)document[i][0]).Text, Is.EqualTo(new[] { "A", "B", "C" }[i]));
		}
		Assert.That(document.Text, Is.EqualTo("A\nB\nC"));
	}

	[Test]
	public void ToggleOffPartialListShouldLeaveRemainingItemsAsListAndConvertSelectedItemsToParagraphs()
	{
		// Ordered list A, B, C, D. Select B and C → toggle off ordered.
		// Expected: ol(A) | para(B) | para(C) | ol(D)
		var html = "<ol><li>A</li><li>B</li><li>C</li><li>D</li></ol>";
		var document = new Document();
		Assert.That(DocumentFormat.Html.LoadFromString(document.DocumentRange, html), Is.True);
		Assert.That(document.IsValid(), Is.True);

		var list = (ListElement)document[0];
		var itemB = list[1];
		var itemC = list[2];
		var range = document.GetRange(itemB.DocumentStart, itemC.DocumentStart + itemC.Length);
		range.ReplaceWithList(ListType.Ordered);

		Assert.That(document.IsValid(), Is.True);
		Assert.That(document.Count, Is.EqualTo(4), "Should have ol(A) + para(B) + para(C) + ol(D)");

		Assert.That(document[0], Is.TypeOf<ListElement>(), "First block should be a list");
		Assert.That(((TextElement)((ListElement)document[0])[0][0]).Text, Is.EqualTo("A"));

		Assert.That(document[1], Is.TypeOf<ParagraphElement>());
		Assert.That(document[1], Is.Not.TypeOf<ListItemElement>());
		Assert.That(((TextElement)document[1][0]).Text, Is.EqualTo("B"));

		Assert.That(document[2], Is.TypeOf<ParagraphElement>());
		Assert.That(document[2], Is.Not.TypeOf<ListItemElement>());
		Assert.That(((TextElement)document[2][0]).Text, Is.EqualTo("C"));

		Assert.That(document[3], Is.TypeOf<ListElement>(), "Last block should be a list");
		Assert.That(((TextElement)((ListElement)document[3])[0][0]).Text, Is.EqualTo("D"));
	}

	[Test]
	public void ToggleOffSingleItemListShouldProduceOneParagraph()
	{
		var html = "<ul><li>Hello</li></ul>";
		var document = new Document();
		Assert.That(DocumentFormat.Html.LoadFromString(document.DocumentRange, html), Is.True);
		Assert.That(document.IsValid(), Is.True);

		var list = (ListElement)document[0];
		var range = document.GetRange(list.DocumentStart, list.DocumentStart + list.Length);
		range.ReplaceWithList(ListType.Unordered);

		Assert.That(document.IsValid(), Is.True);
		Assert.That(document.Count, Is.EqualTo(1));
		Assert.That(document[0], Is.TypeOf<ParagraphElement>());
		Assert.That(document[0], Is.Not.TypeOf<ListItemElement>());
		Assert.That(((TextElement)document[0][0]).Text, Is.EqualTo("Hello"));
	}

	[Test]
	public void ToggleOffMultipleAdjacentSameTypeListsShouldConvertAllToParagraphs()
	{
		// Two ordered lists (would have been merged if created together, but created separately here).
		// Selecting both and toggling ordered should convert all items to paragraphs.
		var html = "<ol><li>A</li><li>B</li></ol><ol><li>C</li><li>D</li></ol>";
		var document = new Document();
		Assert.That(DocumentFormat.Html.LoadFromString(document.DocumentRange, html), Is.True);
		Assert.That(document.IsValid(), Is.True);

		// Select the full document range (both lists).
		var range = document.GetRange(0, document.Length);
		range.ReplaceWithList(ListType.Ordered);

		Assert.That(document.IsValid(), Is.True);
		Assert.That(document.Count, Is.EqualTo(4), "All four items should become plain paragraphs");
		var expectedTexts = new[] { "A", "B", "C", "D" };
		for (int i = 0; i < 4; i++)
		{
			Assert.That(document[i], Is.TypeOf<ParagraphElement>());
			Assert.That(document[i], Is.Not.TypeOf<ListItemElement>());
			Assert.That(((TextElement)document[i][0]).Text, Is.EqualTo(expectedTexts[i]));
		}
	}

	[Test]
	public void ToggleOffDoesNotTriggerWhenSelectionContainsMixedTypes()
	{
		// Selection contains an ordered list AND a paragraph → should convert to ordered, not toggle off.
		var html = "<p>Intro</p><ol><li>A</li><li>B</li></ol>";
		var document = new Document();
		Assert.That(DocumentFormat.Html.LoadFromString(document.DocumentRange, html), Is.True);
		Assert.That(document.IsValid(), Is.True);
		Assert.That(document.Count, Is.EqualTo(2));

		var range = document.GetRange(0, document.Length);
		range.ReplaceWithList(ListType.Ordered);

		Assert.That(document.IsValid(), Is.True);
		// All content should now be in an ordered list, NOT converted to paragraphs.
		Assert.That(document.Count, Is.EqualTo(1), "Should be one merged ordered list");
		Assert.That(document[0], Is.TypeOf<ListElement>());
		Assert.That(((ListElement)document[0]).Count, Is.EqualTo(3), "List should have Intro, A, B");
	}

	[Test]
	public void ToggleOffDoesNotTriggerWhenSelectionContainsDifferentListType()
	{
		// Selecting an unordered list but calling ReplaceWithList with ordered → convert, don't toggle.
		var html = "<ul><li>A</li><li>B</li></ul>";
		var document = new Document();
		Assert.That(DocumentFormat.Html.LoadFromString(document.DocumentRange, html), Is.True);
		Assert.That(document.IsValid(), Is.True);

		var list = (ListElement)document[0];
		var range = document.GetRange(list.DocumentStart, list.DocumentStart + list.Length);
		range.ReplaceWithList(ListType.Ordered); // different type

		Assert.That(document.IsValid(), Is.True);
		Assert.That(document.Count, Is.EqualTo(1), "Should be one ordered list (converted, not toggled)");
		Assert.That(document[0], Is.TypeOf<ListElement>());
		var resultList = (ListElement)document[0];
		Assert.That(((MultipleListType)resultList.Type).Types[0], Is.TypeOf<NumericListType>(), "Result should be ordered/numeric");
		Assert.That(resultList.Count, Is.EqualTo(2));
	}

	// ── tab ↔ indent-level round-trip tests ─────────────────────────────────

	[Test]
	public void ParagraphWithLeadingTabsShouldBecomeIndentedListItem()
	{
		// A paragraph starting with two tabs should become a level-2 list item with no leading tabs.
		var document = new Document();
		document.InsertText(0, "\t\tHello");
		Assert.That(document.Count, Is.EqualTo(1));
		Assert.That(document[0], Is.TypeOf<ParagraphElement>());

		var range = document.GetRange(0, document.Length);
		range.ReplaceWithList(ListType.Unordered);

		Assert.That(document.IsValid(), Is.True);
		Assert.That(document.Count, Is.EqualTo(1));
		Assert.That(document[0], Is.TypeOf<ListElement>());
		var list = (ListElement)document[0];
		Assert.That(list.Count, Is.EqualTo(1));
		var item = list[0];
		Assert.That(item.Level, Is.EqualTo(2), "Level should be 2 from the two leading tabs");
		Assert.That(((TextElement)item[0]).Text, Is.EqualTo("Hello"), "Leading tabs should have been removed from the text");
	}

	[Test]
	public void ParagraphWithNoLeadingTabsShouldBecomeLevel0ListItem()
	{
		var document = new Document();
		document.InsertText(0, "Hello");

		var range = document.GetRange(0, document.Length);
		range.ReplaceWithList(ListType.Unordered);

		Assert.That(document.IsValid(), Is.True);
		var list = (ListElement)document[0];
		Assert.That(list[0].Level, Is.EqualTo(0));
		Assert.That(((TextElement)list[0][0]).Text, Is.EqualTo("Hello"));
	}

	[Test]
	public void IndentedListItemShouldBecomeTabPrefixedParagraphOnToggleOff()
	{
		// Build a level-2 item by converting a paragraph with two leading tabs.
		var document = new Document();
		document.InsertText(0, "\t\tWorld");

		var range = document.GetRange(0, document.Length);
		range.ReplaceWithList(ListType.Unordered);

		Assert.That(document.IsValid(), Is.True);
		var list = (ListElement)document[0];
		Assert.That(list[0].Level, Is.EqualTo(2), "Should be a level-2 item after conversion");

		// Toggle off
		range = document.GetRange(list.DocumentStart, list.DocumentStart + list.Length);
		range.ReplaceWithList(ListType.Unordered);

		Assert.That(document.IsValid(), Is.True);
		Assert.That(document.Count, Is.EqualTo(1));
		Assert.That(document[0], Is.TypeOf<ParagraphElement>());
		Assert.That(document[0], Is.Not.TypeOf<ListItemElement>());
		Assert.That(document[0].Text, Is.EqualTo("\t\tWorld"), "Two leading tabs should have been prepended");
	}

	[Test]
	public void TabToParagraphAndBackShouldRoundTrip()
	{
		// Paragraph with one leading tab → list item (level 1) → toggle off → back to paragraph with one tab.
		var document = new Document();
		document.InsertText(0, "\tFoo");

		var range = document.GetRange(0, document.Length);
		range.ReplaceWithList(ListType.Unordered);

		Assert.That(document.IsValid(), Is.True);
		var list = (ListElement)document[0];
		Assert.That(list[0].Level, Is.EqualTo(1));
		Assert.That(((TextElement)list[0][0]).Text, Is.EqualTo("Foo"));

		// Toggle off
		range = document.GetRange(list.DocumentStart, list.DocumentStart + list.Length);
		range.ReplaceWithList(ListType.Unordered);

		Assert.That(document.IsValid(), Is.True);
		Assert.That(document.Count, Is.EqualTo(1));
		Assert.That(document[0], Is.TypeOf<ParagraphElement>());
		Assert.That(document[0], Is.Not.TypeOf<ListItemElement>());
		Assert.That(document[0].Text, Is.EqualTo("\tFoo"), "Should round-trip back to the original text with leading tab");
	}

	[Test]
	public void ToggleOffLastItemsShouldKeepParagraphBelow()
	{
		// List followed by a blank paragraph
		var html = "<ul><li>Item 1</li><li>Item 2</li><li></li></ul><p></p>";
		var document = new Document();
		Assert.That(DocumentFormat.Html.LoadFromString(document.DocumentRange, html), Is.True);
		Assert.That(document.IsValid(), Is.True);
		Assert.That(document.Count, Is.EqualTo(2), "Should start with list and blank paragraph");

		// Toggle the last two items in the list off (to paragraphs)
		var list = (ListElement)document[0];
		var item2 = list[1];
		var item3 = list[2];
		var range = document.GetRange(item2.DocumentStart, item3.DocumentStart + item3.Length);
		// Toggle off
		range.ReplaceWithList(ListType.Unordered);

		Assert.That(document.IsValid(), Is.True, "Document should be valid after toggle");
		Assert.That(document.Count, Is.EqualTo(4), "Should have list, paragraph, blank paragraph");
		Assert.That(document[0], Is.TypeOf<ListElement>(), "First element should be a list");
		Assert.That(document[1], Is.TypeOf<ParagraphElement>(), "Second element should be a paragraph (was list item)");
		Assert.That(document[2], Is.TypeOf<ParagraphElement>(), "Third element should be a paragraph (was list item)");
		Assert.That(document[3], Is.TypeOf<ParagraphElement>(), "Fourth element should be the blank paragraph");
		var firstList = (ListElement)document[0];
		Assert.That(firstList.Count, Is.EqualTo(1), "First list should have one item");
		Assert.That(((TextElement)firstList[0][0]).Text, Is.EqualTo("Item 1"));
		var secondPara = (ParagraphElement)document[1];
		Assert.That(secondPara.Count, Is.EqualTo(1), "Second paragraph should have one inline element");
		Assert.That(((TextElement)secondPara[0]).Text, Is.EqualTo("Item 2"));
		var thirdPara = (ParagraphElement)document[2];
		Assert.That(thirdPara.Count, Is.EqualTo(0), "Third paragraph should have no inline elements");
		Assert.That(thirdPara.Text, Is.EqualTo(""), "Third paragraph should be empty");
		var fourthPara = (ParagraphElement)document[3];
		Assert.That(fourthPara.Count, Is.EqualTo(0), "Fourth paragraph should have no inline elements");
		Assert.That(fourthPara.Text, Is.EqualTo(""), "Fourth paragraph should remain empty");
	}

	[Test]
	public void ToggleOffWholeListShouldKeepParagraphBelow()
	{
		// List followed by a blank paragraph
		var html = "<ul><li>Item 1</li><li>Item 2</li><li>Item 3</li></ul><p></p>";
		var document = new Document();
		Assert.That(DocumentFormat.Html.LoadFromString(document.DocumentRange, html), Is.True);
		Assert.That(document.IsValid(), Is.True);
		Assert.That(document.Count, Is.EqualTo(2), "Should start with list and blank paragraph");

		// Toggle off the whole list
		var list = (ListElement)document[0];
		var range = document.GetRange(list.DocumentStart, list.DocumentStart + list.Length);
		// Toggle off
		range.ReplaceWithList(ListType.Unordered);

		Assert.That(document.IsValid(), Is.True, "Document should be valid after toggle");
		Assert.That(document.Count, Is.EqualTo(4), "Should have three paragraphs and blank paragraph");
		Assert.That(document[0], Is.TypeOf<ParagraphElement>(), "First element should be a paragraph");
		Assert.That(document[1], Is.TypeOf<ParagraphElement>(), "Second element should be a paragraph");
		Assert.That(document[2], Is.TypeOf<ParagraphElement>(), "Third element should be a paragraph");
		Assert.That(document[3], Is.TypeOf<ParagraphElement>(), "Fourth element should be the blank paragraph");
		Assert.That(((TextElement)document[0][0]).Text, Is.EqualTo("Item 1"));
		Assert.That(((TextElement)document[1][0]).Text, Is.EqualTo("Item 2"));
		Assert.That(((TextElement)document[2][0]).Text, Is.EqualTo("Item 3"));
		var blankPara = (ParagraphElement)document[3];
		Assert.That(blankPara.Count, Is.EqualTo(0), "Blank paragraph should have no inline elements");
		Assert.That(blankPara.Text, Is.EqualTo(""), "Blank paragraph should remain empty");
	}

	[Test]
	public void ChangeListTypeShouldKeepParagraphBelow()
	{
		// List followed by a blank paragraph
		var html = "<ul><li>Item 1</li><li>Item 2</li><li>Item 3</li></ul><p></p>";
		var document = new Document();
		Assert.That(DocumentFormat.Html.LoadFromString(document.DocumentRange, html), Is.True);
		Assert.That(document.IsValid(), Is.True);
		Assert.That(document.Count, Is.EqualTo(2), "Should start with list and blank paragraph");

		// Change the list type to ordered
		var list = (ListElement)document[0];
		var range = document.GetRange(list.DocumentStart, list.DocumentStart + list.Length);
		// Change type
		range.ReplaceWithList(ListType.Ordered);

		Assert.That(document.IsValid(), Is.True, "Document should be valid after change");
		Assert.That(document.Count, Is.EqualTo(2), "Should have list and blank paragraph");
		Assert.That(document[0], Is.TypeOf<ListElement>(), "First element should be a list");
		Assert.That(document[1], Is.TypeOf<ParagraphElement>(), "Second element should be the blank paragraph");
		var orderedList = (ListElement)document[0];
		Assert.That(orderedList.Count, Is.EqualTo(3), "List should have three items");
		Assert.That(((TextElement)orderedList[0][0]).Text, Is.EqualTo("Item 1"));
		Assert.That(((TextElement)orderedList[1][0]).Text, Is.EqualTo("Item 2"));
		Assert.That(((TextElement)orderedList[2][0]).Text, Is.EqualTo("Item 3"));
		var blankPara = (ParagraphElement)document[1];
		Assert.That(blankPara.Count, Is.EqualTo(0), "Blank paragraph should have no inline elements");
		Assert.That(blankPara.Text, Is.EqualTo(""), "Blank paragraph should remain empty");
	}
}