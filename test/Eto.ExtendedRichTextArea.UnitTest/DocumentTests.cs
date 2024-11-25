using Eto.ExtendedRichTextArea.Model;
using Eto.Forms;

namespace Eto.ExtendedRichTextArea.UnitTest;

public class TestBase
{
	[SetUp]
	public void Setup()
	{
		if (Application.Instance == null)
		{
			Platform platform;
			if (EtoEnvironment.Platform.IsMac)
				platform = new Eto.Mac.Platform();
			// else if (EtoEnvironment.Platform.IsGtk)
			// 	platform = new Eto.GtkSharp.Platform();
			// else if (EtoEnvironment.Platform.IsWinForms)
			// 	platform = new Eto.WinForms.Platform();
			// else if (EtoEnvironment.Platform.IsWindows)
			// 	platform = new Eto.Wpf.Platform();
			else
				throw new NotSupportedException("Platform not supported");
			var app = new Application(platform);
			app.Attach();
		}
	}

	[TearDown]
	public void TearDown()
	{
	}	
}

public class DocumentTests : TestBase
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
		Assert.That(document[0][0][0], Is.TypeOf<Span>(), "Inline element should be a span");
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
		Assert.That(document[0][0][0], Is.TypeOf<Span>(), "Inline element should be a span");
		Assert.That(document[1][0][0], Is.TypeOf<Span>(), "Inline element should be a span");
	}
	
	[Test]
	public void InseertTextInExistingMultiLineTextShouldHaveCorrectResults()
	{
		var document = new Document();
		document.InsertText(0, "Hello\nWorld");
		document.InsertText(6, "There ");
		Assert.That(document.Text, Is.EqualTo("Hello\nThere World"));
		Assert.That(document.Length, Is.EqualTo(17));
		Assert.That(document.Count, Is.EqualTo(2), "Should be two paragraphs");
		Assert.That(document[0].Count, Is.EqualTo(1), "Should only be one run in first paragraph");
		Assert.That(document[1].Count, Is.EqualTo(1), "Should only be one run in second paragraph");
		Assert.That(document[0][0].Count, Is.EqualTo(1), "Should only be one inline element in first paragraph");
		Assert.That(document[1][0].Count, Is.EqualTo(1), "Should only be one inline element in second paragraph");
		Assert.That(document[0][0][0], Is.TypeOf<Span>(), "Inline element should be a span");
		Assert.That(document[1][0][0], Is.TypeOf<Span>(), "Inline element should be a span");
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
		Assert.That(document.Count, Is.EqualTo(2), "Should be two paragraphs");
		Assert.That(document[0].Count, Is.EqualTo(1), "Should only be one run in first paragraph");
		Assert.That(document[1].Count, Is.EqualTo(1), "Should only be one run in second paragraph");
		Assert.That(document[0][0].Count, Is.EqualTo(1), "Should only be one inline element in first paragraph");
		Assert.That(document[1][0].Count, Is.EqualTo(1), "Should only be one inline element in second paragraph");
		Assert.That(document[0][0][0], Is.TypeOf<Span>(), "Inline element should be a span");
		Assert.That(document[1][0][0], Is.TypeOf<Span>(), "Inline element should be a span");
	}
}
