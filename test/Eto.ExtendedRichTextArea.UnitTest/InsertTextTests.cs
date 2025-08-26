using Eto.Drawing;
using Eto.ExtendedRichTextArea.Model;

namespace Eto.ExtendedRichTextArea.UnitTest;

public class InsertTextTests : TestBase
{

    [Test]
    public void InsertSingleLineTextShouldHaveCorrectResults()
    {
        var document = new Document();
		document.InsertText(0, "Hello");
		Assert.That(document.Text, Is.EqualTo("Hello"));
		Assert.That(document.Length, Is.EqualTo(5));
		Assert.That(document.Count, Is.EqualTo(1), "Should only be one paragraph");
		Assert.That(document[0].Count, Is.EqualTo(1), "Should only be one inline element");
		Assert.That(document[0][0], Is.TypeOf<TextElement>(), "Inline element should be a span");
    }
	
	[Test]
	public void InsertMultiLineTextShouldHaveCorrectResults()
	{
		var document = new Document();
		document.InsertText(0, "Hello\nWorld");
		Assert.That(document.Text, Is.EqualTo("Hello\nWorld"));
		Assert.That(document.Length, Is.EqualTo(11));
		Assert.That(document.Count, Is.EqualTo(2), "Should be two paragraphs");
		Assert.That(document[0].Count, Is.EqualTo(1), "Should only be one inline element in first paragraph");
		Assert.That(document[1].Count, Is.EqualTo(1), "Should only be one inline element in second paragraph");
		Assert.That(document[0][0], Is.TypeOf<TextElement>(), "Inline element should be a text element");
		Assert.That(document[1][0], Is.TypeOf<TextElement>(), "Inline element should be a text element");
	}
	
	[Test]
	public void InsertTextInExistingMultiLineTextShouldHaveCorrectResults()
	{
		var document = new Document();
		document.InsertText(0, "Hello\nWorld");
		Assert.That(document.Text, Is.EqualTo("Hello\nWorld"));
		document.InsertText(6, "There ");
		Assert.That(document.Text, Is.EqualTo("Hello\nThere World"));
		Assert.That(document.Length, Is.EqualTo(17));
		Assert.That(document.Count, Is.EqualTo(2), "Should be two paragraphs");
		Assert.That(document[0].Count, Is.EqualTo(1), "Should only be one inline element in first paragraph");
		Assert.That(document[1].Count, Is.EqualTo(1), "Should only be one inline element in second paragraph");
		Assert.That(document[0][0], Is.TypeOf<TextElement>(), "Inline element should be a text element");
		Assert.That(document[1][0], Is.TypeOf<TextElement>(), "Inline element should be a text element");
	}
	
	[Test]
	public void InsertTextWithNewLinesInExistingMultiLineTextShouldHaveCorrectResults()
	{
		var document = new Document();
		document.InsertText(0, "Hello\nWorld");
		Assert.That(document.Text, Is.EqualTo("Hello\nWorld"));

		document.InsertText(6, "There\nFriendly\n");
		Assert.That(document.Text, Is.EqualTo("Hello\nThere\nFriendly\nWorld"));
		Assert.That(document.Length, Is.EqualTo(26));
		Assert.That(document.Count, Is.EqualTo(4), "Should be four paragraphs");
		Assert.That(document[0].Count, Is.EqualTo(1), "Should only be one inline element in first paragraph");
		Assert.That(document[1].Count, Is.EqualTo(1), "Should only be one inline element in second paragraph");
		Assert.That(document[2].Count, Is.EqualTo(1), "Should only be one inline element in third paragraph");
		Assert.That(document[3].Count, Is.EqualTo(1), "Should only be one inline element in fourth paragraph");
		Assert.That(document[0][0], Is.TypeOf<TextElement>(), "Inline element should be a text element");
		Assert.That(document[1][0], Is.TypeOf<TextElement>(), "Inline element should be a text element");
		Assert.That(document[2][0], Is.TypeOf<TextElement>(), "Inline element should be a text element");
		Assert.That(document[3][0], Is.TypeOf<TextElement>(), "Inline element should be a text element");
	}
	
	[Test]
	public void InsertingNewlineShouldOnlyAddOneParagraph()
	{
		var document = new Document();
		document.InsertText(0, "Hello\nWorld");
		Assert.That(document.Text, Is.EqualTo("Hello\nWorld"));
		
		document.InsertText(2, "\n");
		Assert.That(document.Text, Is.EqualTo("He\nllo\nWorld"));
	}

	[TestCase("Hello\nWorld", 2, "\n", "He\nllo\nWorld")]
	[TestCase("Hello\nWorld", 4, "\nasdf", "Hell\nasdfo\nWorld")]
	[TestCase("Hello", 3, "\nasdf", "Hel\nasdflo")]
	[TestCase("Hello\n\nWorld", 5, "\n", "Hello\n\n\nWorld")]
	[TestCase("Hello\n\nWorld", 6, "\n", "Hello\n\n\nWorld")]
	[TestCase("Hello\n\nWorld", 7, "\n", "Hello\n\n\nWorld")]
	[TestCase("Hello\n\nWorld", 6, "asdf", "Hello\nasdf\nWorld")]
	[TestCase("", 0, "exercitation\namet eu nostrud qui enim\nexercitation nisi culpa dolore\nut duis proident reprehenderit consectetur\nin esse cillum officia eu irure", "exercitation\namet eu nostrud qui enim\nexercitation nisi culpa dolore\nut duis proident reprehenderit consectetur\nin esse cillum officia eu irure")]
	[TestCase("\n\n\n", 1, "exercitation\namet\nnisi\nut\nin", "\nexercitation\namet\nnisi\nut\nin\n\n")]
	[TestCase("Hello\nWorld", 0, "\n", "\nHello\nWorld")]
	public void InsertingTextShouldWork(string initialText, int insertIndex, string text, string expected)
	{
		var document = new Document();
		document.Text = initialText;
		Assert.That(document.Text, Is.EqualTo(initialText));
		// document.BeginEdit();
		document.InsertText(insertIndex, text);
		// document.EndEdit();
		Assert.That(document.Text, Is.EqualTo(expected));
	}
	
	[Test]
	public void InsertingWithDifferentFontShouldWork()
	{
		var document = new Document();
		document.InsertText(0, "Hello", new Attributes { Font = new Font("Arial", 12) });
		Assert.That(document.Text, Is.EqualTo("Hello"));
		Assert.That(document.Length, Is.EqualTo(5));
		Assert.That(document.Count, Is.EqualTo(1), "Should only be one paragraph");
		Assert.That(document[0].Count, Is.EqualTo(1), "Should only be one inline element");
		Assert.That(document[0][0], Is.TypeOf<TextElement>(), "Inline element should be a span");
		Assert.That(((TextElement)document[0][0]).Attributes?.Font?.FamilyName, Is.EqualTo("Arial"));
	}
	
	
	[TestCase("<p style=\"font-family: Courier New\">Hello</p><p style=\"font-family: Courier New\">There</p>", 4, "\n\n", 0, "Courier New")]
	[TestCase("<p style=\"font-family: Courier New\">Hello</p><p style=\"font-family: Courier New\">There</p>", 4, "\n\n", 4, "Courier New")]
	[TestCase("<p style=\"font-family: Courier New\">Hello</p><p style=\"font-family: Courier New\">There</p>", 4, "\n\n", 5, "Courier New")]
	[TestCase("<p style=\"font-family: Courier New\">Hello</p><p style=\"font-family: Courier New\">There</p>", 4, "\n\n", 6, "Courier New")]
	[TestCase("<p style=\"font-family: Courier New\">Hello</p><p style=\"font-family: Courier New\">There</p>", 4, "\n\n", 7, "Courier New")]
	[TestCase("<p style=\"font-family: Courier New\">Hello</p><p style=\"font-family: Courier New\">There</p>", 4, "\n\n", 8, "Courier New")]
	public void InsertingParagraphWithFormattingShouldKeepFormatting(string html, int insertIndex, string insertText, int testIndex, string fontFamily)
	{
		var document = new Document();
		new HtmlParser(document).ParseHtml(html);

		document.InsertText(insertIndex, insertText);
		var attributes = document.GetAttributes(testIndex, testIndex);
		Assert.That(attributes?.Family?.Name, Is.EqualTo(fontFamily));
		
	}
}
