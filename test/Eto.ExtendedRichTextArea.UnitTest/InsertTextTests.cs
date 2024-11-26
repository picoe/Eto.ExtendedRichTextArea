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
		Assert.That(document[0].Count, Is.EqualTo(1), "Should only be one run");
		Assert.That(document[0][0].Count, Is.EqualTo(1), "Should only be one inline element");
		Assert.That(document[0][0][0], Is.TypeOf<SpanElement>(), "Inline element should be a span");
    }
	
	[Test]
	public void InsertMultiLineTextShouldHaveCorrectResults()
	{
		var document = new Document();
		document.InsertText(0, "Hello\nWorld");
		Assert.That(document.Text, Is.EqualTo("Hello\nWorld"));
		Assert.That(document.Length, Is.EqualTo(11));
		Assert.That(document.Count, Is.EqualTo(2), "Should be two paragraphs");
		Assert.That(document[0].Count, Is.EqualTo(1), "Should only be one run in first paragraph");
		Assert.That(document[1].Count, Is.EqualTo(1), "Should only be one run in second paragraph");
		Assert.That(document[0][0].Count, Is.EqualTo(1), "Should only be one inline element in first paragraph");
		Assert.That(document[1][0].Count, Is.EqualTo(1), "Should only be one inline element in second paragraph");
		Assert.That(document[0][0][0], Is.TypeOf<SpanElement>(), "Inline element should be a span");
		Assert.That(document[1][0][0], Is.TypeOf<SpanElement>(), "Inline element should be a span");
	}
	
	[Test]
	public void InseertTextInExistingMultiLineTextShouldHaveCorrectResults()
	{
		var document = new Document();
		document.InsertText(0, "Hello\nWorld");
		Assert.That(document.Text, Is.EqualTo("Hello\nWorld"));
		document.InsertText(6, "There ");
		Assert.That(document.Text, Is.EqualTo("Hello\nThere World"));
		Assert.That(document.Length, Is.EqualTo(17));
		Assert.That(document.Count, Is.EqualTo(2), "Should be two paragraphs");
		Assert.That(document[0].Count, Is.EqualTo(1), "Should only be one run in first paragraph");
		Assert.That(document[1].Count, Is.EqualTo(1), "Should only be one run in second paragraph");
		Assert.That(document[0][0].Count, Is.EqualTo(1), "Should only be one inline element in first paragraph");
		Assert.That(document[1][0].Count, Is.EqualTo(1), "Should only be one inline element in second paragraph");
		Assert.That(document[0][0][0], Is.TypeOf<SpanElement>(), "Inline element should be a span");
		Assert.That(document[1][0][0], Is.TypeOf<SpanElement>(), "Inline element should be a span");
	}
	
	[Test]
	public void InseertTextWithNewLinesInExistingMultiLineTextShouldHaveCorrectResults()
	{
		var document = new Document();
		document.InsertText(0, "Hello\nWorld");
		Assert.That(document.Text, Is.EqualTo("Hello\nWorld"));

		document.InsertText(6, "There\nFriendly\n");
		Assert.That(document.Text, Is.EqualTo("Hello\nThere\nFriendly\nWorld"));
		Assert.That(document.Length, Is.EqualTo(26));
		Assert.That(document.Count, Is.EqualTo(4), "Should be four paragraphs");
		Assert.That(document[0].Count, Is.EqualTo(1), "Should only be one run in first paragraph");
		Assert.That(document[1].Count, Is.EqualTo(1), "Should only be one run in second paragraph");
		Assert.That(document[2].Count, Is.EqualTo(1), "Should only be one run in third paragraph");
		Assert.That(document[3].Count, Is.EqualTo(1), "Should only be one run in fourth paragraph");
		Assert.That(document[0][0].Count, Is.EqualTo(1), "Should only be one inline element in first paragraph");
		Assert.That(document[1][0].Count, Is.EqualTo(1), "Should only be one inline element in second paragraph");
		Assert.That(document[2][0].Count, Is.EqualTo(1), "Should only be one inline element in third paragraph");
		Assert.That(document[3][0].Count, Is.EqualTo(1), "Should only be one inline element in fourth paragraph");
		Assert.That(document[0][0][0], Is.TypeOf<SpanElement>(), "Inline element should be a span");
		Assert.That(document[1][0][0], Is.TypeOf<SpanElement>(), "Inline element should be a span");
		Assert.That(document[2][0][0], Is.TypeOf<SpanElement>(), "Inline element should be a span");
		Assert.That(document[3][0][0], Is.TypeOf<SpanElement>(), "Inline element should be a span");
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
	[TestCase("Hello\n\nWorld", 5, "\n", "Hello\n\n\nWorld")]
	[TestCase("Hello\n\nWorld", 6, "\n", "Hello\n\n\nWorld")]
	[TestCase("Hello\n\nWorld", 7, "\n", "Hello\n\n\nWorld")]
	[TestCase("Hello\n\nWorld", 6, "asdf", "Hello\nasdf\nWorld")]
	public void InsertingTextShouldWork(string initialText, int insertIndex, string text, string expected)
	{
		var document = new Document();
		document.Text = initialText;
		document.InsertText(insertIndex, text);
		Assert.That(document.Text, Is.EqualTo(expected));
	}
}
