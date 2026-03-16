using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Eto.Drawing;
using Eto.ExtendedRichTextArea.Model;
using Eto.Forms;

using NUnit.Framework;

namespace Eto.ExtendedRichTextArea.UnitTest;

public class HtmlTests : TestBase
{

	[Test]
	public void HtmlToDocumentShouldWork()
	{
		var html = "<p>Hello <b>World</b></p><p>This is a test</p>";
		var document = new Document();
		var loaded = DocumentFormat.Html.LoadFromString(document.DocumentRange, html);
		Assert.That(loaded, Is.True);
		Assert.That(document.Length, Is.EqualTo(26));
		Assert.That(document.Text, Is.EqualTo("Hello World\nThis is a test"));
		Assert.That(document[0].Count, Is.EqualTo(2));
		Assert.That(document[1].Count, Is.EqualTo(1));

		Assert.That(document[0][0], Is.TypeOf<TextElement>());
		Assert.That(document[0][1], Is.TypeOf<TextElement>());
		Assert.That(((TextElement)document[0][0])?.ActualAttributes?.Font?.Bold, Is.False);
		Assert.That(((TextElement)document[0][1])?.ActualAttributes?.Font?.Bold, Is.True);
	}

	[Test]
	public void HtmlWriterShouldSkipDefaultForegroundColor()
	{
		var document = new Document();
		document.DefaultForeground = new SolidBrush(Colors.White);
		document.InsertText(0, "Hello");

		var html = DocumentFormat.Html.SaveToString(document.DocumentRange);

		Assert.That(html, Does.Not.Contain("color:#FFFFFF"));
	}

	[Test]
	public void HtmlWriterShouldWriteTabsAsHtmlTabEntities()
	{
		var document = new Document();
		document.InsertText(0, "\ttry\tvalue");

		var html = DocumentFormat.Html.SaveToString(document.DocumentRange);

		Assert.That(html, Does.Contain("&#9;try&#9;value"));
		Assert.That(html, Does.Contain("white-space:pre"));
		Assert.That(html, Does.Not.Contain("\t"));
	}

	[Test]
	public void HtmlWriterAndReaderShouldPreserveTabsWhenTabsAreSeparateTextElements()
	{
		var source = new Document();
		source.InsertText(0, "\t");
		source.InsertText(source.Length, "try", new Attributes { Bold = true });
		source.InsertText(source.Length, "\t");
		source.InsertText(source.Length, "value", new Attributes { Italic = true });

		var html = DocumentFormat.Html.SaveToString(source.DocumentRange);
		Assert.That(html, Does.Contain("&#9;"));

		var destination = new Document();
		var loaded = DocumentFormat.Html.LoadFromString(destination.DocumentRange, html);

		Assert.That(loaded, Is.True);
		Assert.That(destination.Text, Is.EqualTo("\ttry\tvalue"));
		Assert.That(destination[0].OfType<TextElement>().Any(r => r.Text.Contains('\t')), Is.True);
	}

	[Test]
	public void HtmlWriterShouldPreserveIndentedSpaceRunsFromPreStyledHtmlOnSecondRoundtrip()
	{
		var externalHtml = "<meta charset='utf-8'><div style=\"white-space: pre;\"><div><span style=\"color:#cccccc;\">    </span><span style=\"color:#569cd6;\">public</span><span style=\"color:#cccccc;\"> </span><span style=\"color:#569cd6;\">override</span></div><div><span style=\"color:#cccccc;\">    {</span></div><div><span style=\"color:#cccccc;\">        </span><span style=\"color:#569cd6;\">var</span><span style=\"color:#cccccc;\"> html = SaveToString(range);</span></div><div><span style=\"color:#cccccc;\">    }</span></div></div>";

		var firstPaste = new Document();
		var firstLoaded = DocumentFormat.Html.LoadFromString(firstPaste.DocumentRange, externalHtml);
		Assert.That(firstLoaded, Is.True);

		var copiedHtml = DocumentFormat.Html.SaveToString(firstPaste.DocumentRange);
		Assert.That(copiedHtml, Does.Contain("white-space:pre"));

		var secondPaste = new Document();
		var secondLoaded = DocumentFormat.Html.LoadFromString(secondPaste.DocumentRange, copiedHtml);
		Assert.That(secondLoaded, Is.True);

		Assert.That(secondPaste.Text, Is.EqualTo(firstPaste.Text));
		Assert.That(secondPaste.Text, Does.StartWith("    public override"));
		Assert.That(secondPaste.Text, Does.Contain("\n    {\n"));
		Assert.That(secondPaste.Text, Does.Contain("\n        var html = SaveToString(range);\n"));
	}

	[Test]
	public void HtmlRoundtripShouldPreserveLeadingTabs()
	{
		var source = new Document();
		source.InsertText(0, "\ttry\n\t{\n\t\tvar value = 1;\n\t}");

		var html = DocumentFormat.Html.SaveToString(source.DocumentRange);
		var destination = new Document();
		var loaded = DocumentFormat.Html.LoadFromString(destination.DocumentRange, html);

		Assert.That(loaded, Is.True);
		Assert.That(destination.Text, Is.EqualTo("\ttry\n\t{\n\t\tvar value = 1;\n\t}"));
	}

	[Test]
	public void HtmlRoundtripShouldPreserveTabsInStyledAndInlineText()
	{
		var source = new Document();
		source.InsertText(0, "\tif", new Attributes { Bold = true });
		source.InsertText(source.Length, "\t(x\t>\t0)");

		var html = DocumentFormat.Html.SaveToString(source.DocumentRange);
		var destination = new Document();
		var loaded = DocumentFormat.Html.LoadFromString(destination.DocumentRange, html);

		Assert.That(loaded, Is.True);
		Assert.That(destination.Text, Is.EqualTo("\tif\t(x\t>\t0)"));
	}

	[Test]
	public void HtmlReaderShouldKeepTabCharactersInDocumentParagraphAndTextElements()
	{
		var html = "<p><span style=\"white-space:pre\">\ttry\tvalue</span></p>";
		var document = new Document();
		var loaded = DocumentFormat.Html.LoadFromString(document.DocumentRange, html);

		Assert.That(loaded, Is.True);
		Assert.That(document.Text, Is.EqualTo("\ttry\tvalue"));
		Assert.That(document[0].Text, Is.EqualTo("\ttry\tvalue"));

		var runs = document[0].OfType<TextElement>().ToList();
		Assert.That(runs.Count, Is.GreaterThan(0));
		Assert.That(runs.Any(r => r.Text.Contains('\t')), Is.True);
	}

	[Test]
	public void HtmlReaderShouldKeepTabsWithoutPreWhitespaceStyle()
	{
		var html = "<p>\ttry\tvalue</p>";
		var document = new Document();
		var loaded = DocumentFormat.Html.LoadFromString(document.DocumentRange, html);

		Assert.That(loaded, Is.True);
		Assert.That(document.Text, Is.EqualTo("\ttry\tvalue"));
		Assert.That(document[0].Text, Is.EqualTo("\ttry\tvalue"));
		Assert.That(document[0].OfType<TextElement>().Any(r => r.Text.Contains('\t')), Is.True);
	}

	[Test]
	public void HtmlRoundtripShouldKeepTabCharactersInDocumentParagraphAndTextElements()
	{
		var source = new Document();
		source.InsertText(0, "\ttry\tvalue");

		Assert.That(source.Text, Is.EqualTo("\ttry\tvalue"));
		Assert.That(source[0].Text, Is.EqualTo("\ttry\tvalue"));
		Assert.That(source[0].OfType<TextElement>().Any(r => r.Text.Contains('\t')), Is.True);

		var html = DocumentFormat.Html.SaveToString(source.DocumentRange);
		var destination = new Document();
		var loaded = DocumentFormat.Html.LoadFromString(destination.DocumentRange, html);

		Assert.That(loaded, Is.True);
		Assert.That(destination.Text, Is.EqualTo("\ttry\tvalue"));
		Assert.That(destination[0].Text, Is.EqualTo("\ttry\tvalue"));
		Assert.That(destination[0].OfType<TextElement>().Any(r => r.Text.Contains('\t')), Is.True);
	}

	[Test]
	public void HtmlRoundtripShouldKeepTabsEvenIfPreStyleIsRemoved()
	{
		var source = new Document();
		source.InsertText(0, "\ttry\tvalue");

		var html = DocumentFormat.Html.SaveToString(source.DocumentRange);
		html = html.Replace("white-space:pre", string.Empty, StringComparison.OrdinalIgnoreCase);

		var destination = new Document();
		var loaded = DocumentFormat.Html.LoadFromString(destination.DocumentRange, html);

		Assert.That(loaded, Is.True);
		Assert.That(destination.Text, Is.EqualTo("\ttry\tvalue"));
		Assert.That(destination[0].Text, Is.EqualTo("\ttry\tvalue"));
		Assert.That(destination[0].OfType<TextElement>().Any(r => r.Text.Contains('\t')), Is.True);
	}

	[Test]
	public void HtmlReaderShouldParseClipboardDivsWithTabEntitiesWithoutExtraParagraphs()
	{
		var html = "<meta charset='utf-8'><div><div>&#9;try</div><div>&#9;{</div><div>&#9;&#9;var value = 1;</div><div>&#9;}</div></div>";
		var document = new Document();
		var loaded = DocumentFormat.Html.LoadFromString(document.DocumentRange, html);

		Assert.That(loaded, Is.True);
		Assert.That(document.Count, Is.EqualTo(4));
		Assert.That(document.Text, Is.EqualTo("\ttry\n\t{\n\t\tvar value = 1;\n\t}"));
	}

	[Test]
	public void HtmlReaderShouldNotCreateExtraParagraphForWrapperDiv()
	{
		var html = "<div><div>line 1</div><div>line 2</div></div>";
		var document = new Document();
		var loaded = DocumentFormat.Html.LoadFromString(document.DocumentRange, html);

		Assert.That(loaded, Is.True);
		Assert.That(document.Count, Is.EqualTo(2));
		Assert.That(document.Text, Is.EqualTo("line 1\nline 2"));
	}

	[Test]
	public void HtmlReaderShouldTreatBrOnlyDivAsSingleBlankParagraph()
	{
		var html = "<div><div>line 1</div><div><br></div><div>line 3</div></div>";
		var document = new Document();
		var loaded = DocumentFormat.Html.LoadFromString(document.DocumentRange, html);

		Assert.That(loaded, Is.True);
		Assert.That(document.Count, Is.EqualTo(3));
		Assert.That(document.Text, Is.EqualTo("line 1\n\nline 3"));
	}

	[Test]
	public void HtmlRoundtripShouldPreserveTabsAndBlankParagraphs()
	{
		var source = new Document();
		source.InsertText(0, "\tline 1\n\n\tline 3");

		var html = DocumentFormat.Html.SaveToString(source.DocumentRange);
		var destination = new Document();
		var loaded = DocumentFormat.Html.LoadFromString(destination.DocumentRange, html);

		Assert.That(loaded, Is.True);
		Assert.That(destination.Count, Is.EqualTo(3));
		Assert.That(destination.Text, Is.EqualTo("\tline 1\n\n\tline 3"));
	}

	[Test]
	public void HtmlDataObjectRoundtripShouldPreserveTabsAndBlankParagraphsWhenPastingAtCaret()
	{
		var source = new Document();
		source.InsertText(0, "\tline 1\n\n\tline 3");

		var dataObject = new DataObject();
		DocumentFormat.Html.WriteDataObject(source.DocumentRange, dataObject);

		var destination = new Document();
		destination.InsertText(0, "prefix ");
		var pasteRange = destination.GetRange(destination.Length, destination.Length);

		var loaded = DocumentFormat.Html.ReadDataObject(pasteRange, dataObject);

		Assert.That(loaded, Is.True);
		Assert.That(destination.Text, Is.EqualTo("prefix \tline 1\n\n\tline 3"));
	}

	[Test]
	public void HtmlLoadFromStringShouldPreserveTabsAndBlankParagraphsWhenPastingAtCaret()
	{
		var source = new Document();
		source.InsertText(0, "\tline 1\n\n\tline 3");
		var html = DocumentFormat.Html.SaveToString(source.DocumentRange);

		var destination = new Document();
		destination.InsertText(0, "prefix ");
		var pasteRange = destination.GetRange(destination.Length, destination.Length);

		var loaded = DocumentFormat.Html.LoadFromString(pasteRange, html);

		Assert.That(loaded, Is.True);
		Assert.That(destination.Text, Is.EqualTo("prefix \tline 1\n\n\tline 3"));
	}

	[Test]
	public void HtmlRoundtripSelectionEndingAtParagraphBoundaryShouldNotAddExtraParagraph()
	{
		var source = new Document();
		source.InsertText(0, "\tline 1\n\n\tline 3");
		var selectionEnd = source.Text.IndexOf("\tline 3", StringComparison.Ordinal);
		var selectedRange = source.GetRange(0, selectionEnd);
		var expected = source.Text.Substring(0, selectionEnd);

		var html = DocumentFormat.Html.SaveToString(selectedRange);
		var destination = new Document();
		var loaded = DocumentFormat.Html.LoadFromString(destination.DocumentRange, html);

		Assert.That(loaded, Is.True);
		Assert.That(destination.Text, Is.EqualTo(expected));
	}

	[Test]
	public void HtmlReaderShouldParseWordHtmlFormatting()
	{
		var html = "<html><body> class=MsoNormal><a name=\"OLE_LINK1\"><span lang=EN-US style='mso-ansi-language:\r\nEN-US'>Some <b>bold</b> text</span></a><span lang=EN-US style='mso-ansi-language:\r\nEN-US'><o:p></o:p></span></p>\r\n\r\n<p class=MsoNormal><span lang=EN-US style='mso-ansi-language:EN-US'>Some <i>italic</i>\r\ntext<o:p></o:p></span></p>\r\n\r\n<p class=MsoNormal><span lang=EN-US style='mso-ansi-language:EN-US'>Some <b><i><span\r\nstyle='color:#EE0000'>bold, Red, and Italic</span></i></b><span\r\nstyle='color:#EE0000'> </span>text<o:p></o:p></span></p>\r\n\r\n<!--En</body></html>";
		var document = new Document();
		var loaded = DocumentFormat.Html.LoadFromString(document.DocumentRange, html);

		Assert.That(loaded, Is.True);
		Assert.That(document.Text, Does.Not.Contain("class=MsoNormal>"));
		Assert.That(document.Text, Does.StartWith("Some bold text"));

		var runs = document.SelectMany(block => block.OfType<TextElement>()).ToList();
		Assert.That(runs.Count, Is.GreaterThan(0));

		var italicRun = runs.FirstOrDefault(r => r.Text.Contains("italic", StringComparison.OrdinalIgnoreCase));
		Assert.That(italicRun, Is.Not.Null);
		Assert.That(italicRun!.ActualAttributes?.Font?.Italic, Is.True);

		var redBoldItalicRun = runs.FirstOrDefault(r => r.Text.Contains("bold, Red, and Italic", StringComparison.Ordinal));
		Assert.That(redBoldItalicRun, Is.Not.Null);
		Assert.That(redBoldItalicRun!.ActualAttributes?.Font?.Bold, Is.True);
		Assert.That(redBoldItalicRun.ActualAttributes?.Font?.Italic, Is.True);
		Assert.That(redBoldItalicRun.ActualAttributes?.Foreground, Is.TypeOf<SolidBrush>());
		Assert.That(((SolidBrush)redBoldItalicRun.ActualAttributes!.Foreground!).Color, Is.EqualTo(Color.Parse("#EE0000")));
	}

	[Test]
	public void HtmlReaderShouldOnlyBoldExplicitBoldSegmentInWordSnippet()
	{
		var html = "<html><body> class=MsoNormal><a name=\"OLE_LINK1\"><span lang=EN-US style='mso-ansi-language:\r\nEN-US'>Some <b>bold</b> text</span></a><span lang=EN-US style='mso-ansi-language:\r\nEN-US'><o:p></o:p></span></p>\r\n\r\n<!--En</body></html>";
		var document = new Document();
		var loaded = DocumentFormat.Html.LoadFromString(document.DocumentRange, html);

		Assert.That(loaded, Is.True);
		Assert.That(document.Text, Is.EqualTo("Some bold text"));

		var someAttributes = document.GetAttributes(0, 4);
		var boldAttributes = document.GetAttributes(5, 9);
		var textAttributes = document.GetAttributes(10, 14);

		Assert.That(someAttributes.Bold, Is.False);
		Assert.That(boldAttributes.Bold, Is.True);
		Assert.That(textAttributes.Bold, Is.False);
	}

	[Test]
	public void HtmlReaderShouldPreserveIndentationForPreWhitespaceDivContent()
	{
		var html = "<meta charset='utf-8'><div style=\"color: #cccccc;background-color: #1f1f1f;font-family: Menlo, Monaco, 'Courier New', monospace;font-weight: normal;font-size: 12px;line-height: 18px;white-space: pre;\"><div><span style=\"color: #cccccc;\">        </span><span style=\"color: #c586c0;\">try</span></div><div><span style=\"color: #cccccc;\">        {</span></div><div><span style=\"color: #cccccc;\">            </span><span style=\"color: #569cd6;\">var</span><span style=\"color: #cccccc;\"> </span><span style=\"color: #9cdcfe;\">rtfReader</span><span style=\"color: #cccccc;\"> </span><span style=\"color: #d4d4d4;\">=</span><span style=\"color: #cccccc;\"> </span><span style=\"color: #569cd6;\">new</span><span style=\"color: #cccccc;\"> </span><span style=\"color: #4ec9b0;\">RtfReader</span><span style=\"color: #cccccc;\">();</span></div><div><span style=\"color: #cccccc;\">            </span><span style=\"color: #569cd6;\">var</span><span style=\"color: #cccccc;\"> </span><span style=\"color: #9cdcfe;\">parsed</span><span style=\"color: #cccccc;\"> </span><span style=\"color: #d4d4d4;\">=</span><span style=\"color: #cccccc;\"> </span><span style=\"color: #9cdcfe;\">rtfReader</span><span style=\"color: #d4d4d4;\">.</span><span style=\"color: #dcdcaa;\">ReadDocument</span><span style=\"color: #cccccc;\">(</span><span style=\"color: #9cdcfe;\">text</span><span style=\"color: #cccccc;\">);</span></div><div><span style=\"color: #cccccc;\">            </span><span style=\"color: #569cd6;\">var</span><span style=\"color: #cccccc;\"> </span><span style=\"color: #9cdcfe;\">blocks</span><span style=\"color: #cccccc;\"> </span><span style=\"color: #d4d4d4;\">=</span><span style=\"color: #cccccc;\"> </span><span style=\"color: #9cdcfe;\">parsed</span><span style=\"color: #d4d4d4;\">.</span><span style=\"color: #dcdcaa;\">Select</span><span style=\"color: #cccccc;\">(</span><span style=\"color: #9cdcfe;\">block</span><span style=\"color: #cccccc;\"> </span><span style=\"color: #d4d4d4;\">=&gt;</span><span style=\"color: #cccccc;\"> (</span><span style=\"color: #4ec9b0;\">IBlockElement</span><span style=\"color: #cccccc;\">)</span><span style=\"color: #9cdcfe;\">block</span><span style=\"color: #d4d4d4;\">.</span><span style=\"color: #dcdcaa;\">Clone</span><span style=\"color: #cccccc;\">())</span><span style=\"color: #d4d4d4;\">.</span><span style=\"color: #dcdcaa;\">ToList</span><span style=\"color: #cccccc;\">();</span></div><div><span style=\"color: #cccccc;\">            </span><span style=\"color: #9cdcfe;\">range</span><span style=\"color: #d4d4d4;\">.</span><span style=\"color: #dcdcaa;\">ReplaceWithBlocks</span><span style=\"color: #cccccc;\">(</span><span style=\"color: #9cdcfe;\">blocks</span><span style=\"color: #cccccc;\">);</span></div><div><span style=\"color: #cccccc;\">            </span><span style=\"color: #c586c0;\">return</span><span style=\"color: #cccccc;\"> </span><span style=\"color: #569cd6;\">true</span><span style=\"color: #cccccc;\">;</span></div><div><span style=\"color: #cccccc;\">        }</span></div><div><span style=\"color: #cccccc;\"></span></div></div>";
		var document = new Document();
		var loaded = DocumentFormat.Html.LoadFromString(document.DocumentRange, html);

		Assert.That(loaded, Is.True);
		Assert.That(document.Count, Is.GreaterThanOrEqualTo(8));
		Assert.That(document[0].Text, Is.EqualTo("        try"));
		Assert.That(document[1].Text, Is.EqualTo("        {"));
		Assert.That(document[2].Text, Is.EqualTo("            var rtfReader = new RtfReader();"));
		Assert.That(document[7].Text, Is.EqualTo("        }"));
	}

	[Test]
	public void HtmlReaderShouldIgnoreInterTagFormattingWhitespaceWithPreWrapper()
	{
		var html = """
			<meta charset='utf-8'>
			<div style="white-space: pre;">
			  <div><span>        </span><span>try</span></div>
			  <div><span>        {</span></div>
			  <div><span>            var x = 1;</span></div>
			  <div><span>        }</span></div>
			</div>
			""";

		var document = new Document();
		var loaded = DocumentFormat.Html.LoadFromString(document.DocumentRange, html);

		Assert.That(loaded, Is.True);
		Assert.That(document.Count, Is.EqualTo(4));
		Assert.That(document.Text, Is.EqualTo("        try\n        {\n            var x = 1;\n        }"));
	}

	// ── list indentation tests ──────────────────────────────────────────────

	[Test]
	public void HtmlWriterShouldWriteNestedListTagsForIndentedItems()
	{
		// Build a list with items at levels 0, 1, 2, 1, 0
		var document = new Document();
		DocumentFormat.Html.LoadFromString(document.DocumentRange, "<ul><li>A</li></ul>");
		var list = (ListElement)document[0];
		list[0].Level = 0;
		list.Add(new ListItemElement { Level = 1 });
		list[1].Add(new TextElement { Text = "B" });
		list.Add(new ListItemElement { Level = 2 });
		list[2].Add(new TextElement { Text = "C" });
		list.Add(new ListItemElement { Level = 1 });
		list[3].Add(new TextElement { Text = "D" });
		list.Add(new ListItemElement { Level = 0 });
		list[4].Add(new TextElement { Text = "E" });

		var html = DocumentFormat.Html.SaveToString(document.DocumentRange);

		// The written HTML should contain nested <ul> tags, not margin-left style
		Assert.That(html, Does.Not.Contain("margin-left"));
		Assert.That(html, Does.Contain("<ul>"), "Outer list tag expected");
		// Nested <ul><li> pairs appear inside the outer list
		var ulCount = System.Text.RegularExpressions.Regex.Matches(html, "<ul>").Count;
		Assert.That(ulCount, Is.GreaterThanOrEqualTo(3), "Should have at least 3 nested <ul> openers");
	}

	[Test]
	public void HtmlReaderShouldReadNestedListsAsLeveledItems()
	{
		var html = "<ul><li>A</li><ul><li>B</li><ul><li>C</li></ul><li>D</li></ul><li>E</li></ul>";
		var document = new Document();
		Assert.That(DocumentFormat.Html.LoadFromString(document.DocumentRange, html), Is.True);
		Assert.That(document.IsValid(), Is.True);
		Assert.That(document.Count, Is.EqualTo(1));

		var list = (ListElement)document[0];
		Assert.That(list.Count, Is.EqualTo(5));
		Assert.That(list[0].Level, Is.EqualTo(0));
		Assert.That(list[1].Level, Is.EqualTo(1));
		Assert.That(list[2].Level, Is.EqualTo(2));
		Assert.That(list[3].Level, Is.EqualTo(1));
		Assert.That(list[4].Level, Is.EqualTo(0));
		Assert.That(((TextElement)list[0][0]).Text, Is.EqualTo("A"));
		Assert.That(((TextElement)list[2][0]).Text, Is.EqualTo("C"));
		Assert.That(((TextElement)list[4][0]).Text, Is.EqualTo("E"));
	}

	[Test]
	public void HtmlReaderShouldPreserveOuterListTypeWhenReadingNestedLists()
	{
		// Outer <ol> with nested <ol> — the outer type should be Ordered
		var html = "<ol><li>A</li><ol><li>B</li></ol></ol>";
		var document = new Document();
		Assert.That(DocumentFormat.Html.LoadFromString(document.DocumentRange, html), Is.True);

		var list = (ListElement)document[0];
		Assert.That(((MultipleListType)list.Type).Types[0], Is.TypeOf<NumericListType>(),
			"Outer type should remain Ordered (numeric) after nested list is parsed");
		Assert.That(list[0].Level, Is.EqualTo(0));
		Assert.That(list[1].Level, Is.EqualTo(1));
	}

	[Test]
	public void HtmlRoundtripShouldPreserveListIndentLevels()
	{
		// Build a document with an indented list via the round-trip path
		var sourceHtml = "<ul><li>A</li><ul><li>B</li><ul><li>C</li></ul></ul><li>D</li></ul>";
		var source = new Document();
		Assert.That(DocumentFormat.Html.LoadFromString(source.DocumentRange, sourceHtml), Is.True);

		var serialized = DocumentFormat.Html.SaveToString(source.DocumentRange);

		var destination = new Document();
		Assert.That(DocumentFormat.Html.LoadFromString(destination.DocumentRange, serialized), Is.True);
		Assert.That(destination.IsValid(), Is.True);

		var list = (ListElement)destination[0];
		Assert.That(list.Count, Is.EqualTo(4));
		Assert.That(list[0].Level, Is.EqualTo(0));
		Assert.That(list[1].Level, Is.EqualTo(1));
		Assert.That(list[2].Level, Is.EqualTo(2));
		Assert.That(list[3].Level, Is.EqualTo(0));
		Assert.That(((TextElement)list[0][0]).Text, Is.EqualTo("A"));
		Assert.That(((TextElement)list[1][0]).Text, Is.EqualTo("B"));
		Assert.That(((TextElement)list[2][0]).Text, Is.EqualTo("C"));
		Assert.That(((TextElement)list[3][0]).Text, Is.EqualTo("D"));
	}
}
