namespace Eto.ExtendedRichTextArea.UnitTest;

public class DeleteTextTests : TestBase
{
	[TestCase("<p style='font-family:Courier New'>Hello</p><p style='font-family:Courier New'>There</p><p style='font-family:Courier New'>Friend</p>", 5, 5, 5, 0, "Courier New")]
	[TestCase("<p style='font-family:Courier New'>Hello</p><p style='font-family:Courier New'>T</p><p style='font-family:Courier New'>Friend</p>", 6, 1, 6, 0, "Courier New")]
	public void DeletingTextWithAttributesShouldWork(string html, int deleteIndex, int length, int testIndex, int testLength, string fontFamily)
	{
		var document = new Document();
		var loaded = DocumentFormat.Html.LoadFromString(document.DocumentRange, html);
		Assert.That(loaded, Is.True);

		document.RemoveAt(deleteIndex, length);
		var attributes = document.GetAttributes(testIndex, testIndex + testLength);
		Assert.That(attributes?.Family?.Name, Is.EqualTo(fontFamily));
	}
	
}
