using Eto.ExtendedRichTextArea.Model;

namespace Eto.ExtendedRichTextArea.UnitTest;

public class PlainTextTests : TestBase
{
	[Test]
	public void PlainTextRoundtripShouldPreserveNewlines()
	{
		var source = new Document();
		var text = "First line\nSecond line\nThird line";
		source.InsertText(0, text);

		var serialized = DocumentFormat.PlainText.SaveToString(source.DocumentRange);
		var destination = new Document();
		var loaded = DocumentFormat.PlainText.LoadFromString(destination.DocumentRange, serialized);

		Assert.That(loaded, Is.True);
		Assert.That(serialized, Is.EqualTo(text));
		Assert.That(destination.Text, Is.EqualTo(text));
		Assert.That(destination.Count, Is.EqualTo(3));
	}
}
