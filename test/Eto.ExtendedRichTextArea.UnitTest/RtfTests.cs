using System;
using System.Linq;

using Eto.Drawing;

namespace Eto.ExtendedRichTextArea.UnitTest;

public class RtfTests : TestBase
{
	[Test]
	public void RtfReaderShouldIgnoreFormattingLineBreaks()
	{
		var rtf = "{\\rtf1\\ansi\\ansicpg1252\\deff0{\\fonttbl{\\f0 Arial;}}\n"
			+ "{\\pard\n"
			+ "{\\plain\\f0\\fs24 Hello}\n"
			+ "{\\plain\\f0\\fs24  World}\n"
			+ "}\n"
			+ "}";

		var document = new Document();
		var loaded = DocumentFormat.Rtf.LoadFromString(document.DocumentRange, rtf);

		Assert.That(loaded, Is.True);
		Assert.That(document.Text, Is.EqualTo("Hello World"));
		Assert.That(document.Count, Is.EqualTo(1));
		Assert.That(document[0].Count, Is.GreaterThanOrEqualTo(1));
	}

	[Test]
	public void RtfRoundtripShouldPreserveFontAttributes()
	{
		var source = new Document();
		var defaultFamily = source.DefaultFont.Family;
		var family = Fonts.AvailableFontFamilies
			.FirstOrDefault(f => !string.Equals(f.Name, defaultFamily.Name, StringComparison.OrdinalIgnoreCase))
			?? defaultFamily;
		if (string.Equals(family.Name, defaultFamily.Name, StringComparison.OrdinalIgnoreCase))
			Assert.Inconclusive("No alternate font family available to verify family roundtrip.");
		var regularTypeface = family.Typefaces.FirstOrDefault(tf => !tf.Bold && !tf.Italic)
			?? family.Typefaces.FirstOrDefault(tf => tf.FontStyle == FontStyle.None)
			?? family.Typefaces.First();

		source.InsertText(0, "Hello", new Attributes
		{
			Family = family,
			Typeface = regularTypeface,
			Size = 17,
			Underline = true,
			Strikethrough = true
		});

		var serialized = DocumentFormat.Rtf.SaveToString(source.DocumentRange);
		var destination = new Document();
		var loaded = DocumentFormat.Rtf.LoadFromString(destination.DocumentRange, serialized);

		Assert.That(loaded, Is.True);
		Assert.That(destination.Count, Is.EqualTo(1));
		Assert.That(destination[0].Count, Is.EqualTo(1));

		var element = destination[0][0] as TextElement;
		Assert.That(element, Is.Not.Null);
		Assert.That(element!.Text, Is.EqualTo("Hello"));

		var attributes = element.Attributes;
		Assert.That(attributes, Is.Not.Null);
		Assert.That(attributes!.Family?.Name, Is.EqualTo(family.Name));
		Assert.That(attributes.Size, Is.EqualTo(17).Within(0.01f));
		Assert.That(attributes.Underline, Is.True);
		Assert.That(attributes.Strikethrough, Is.True);
		Assert.That(attributes.Typeface, Is.Not.Null);
		Assert.That(attributes.Typeface!.Bold, Is.False);
		Assert.That(attributes.Typeface.Italic, Is.False);
	}

	[Test]
	public void RtfReaderShouldResolvePostScriptFontNamesFromFlatFontTable()
	{
		var availableFamilies = Fonts.AvailableFontFamilies.ToList();
		var familyForNameLookup = availableFamilies.FirstOrDefault(f => !string.IsNullOrWhiteSpace(f.Name));
		var postscriptTypefaces = availableFamilies
			.SelectMany(f => f.Typefaces)
			.Where(tf => !string.IsNullOrWhiteSpace(tf.PostScriptName))
			.GroupBy(tf => tf.PostScriptName, StringComparer.OrdinalIgnoreCase)
			.Select(g => g.First())
			.Take(2)
			.ToList();

		if (familyForNameLookup is null || postscriptTypefaces.Count < 2)
			Assert.Inconclusive("Insufficient installed fonts/typefaces to validate flat font table postscript resolution.");

		var familyName = familyForNameLookup!.Name;
		var firstPostScriptName = postscriptTypefaces[0].PostScriptName!;
		var secondPostScriptName = postscriptTypefaces[1].PostScriptName!;

		var rtf = "{\\rtf1\\ansi\\ansicpg1252\\cocoartf2868\n"
			+ $"\\cocoatextscaling0\\cocoaplatform0{{\\fonttbl\\f0\\fswiss\\fcharset0 {familyName};\\f1\\fswiss\\fcharset0 {firstPostScriptName};\\f2\\fnil\\fcharset0 {secondPostScriptName};\n"
			+ "}\n"
			+ "{\\colortbl;\\red255\\green255\\blue255;}\n"
			+ "{\\*\\expandedcolortbl;;}\n"
			+ "\\pard\\tx720\\tx1440\\tx2160\\tx2880\\tx3600\\tx4320\\tx5040\\tx5760\\tx6480\\tx7200\\tx7920\\tx8640\\pardirnatural\\partightenfactor0\n"
			+ "\n"
			+ "\\f0\\fs24 \\cf0 Some formatting text\\\n"
			+ "\\\n"
			+ "Oaisjfoiajsdf \n"
			+ "\\f1 aoisj foiasjd f\n"
			+ "\\f2  asodijfoasidjf}";

		var document = new Document();
		var loaded = DocumentFormat.Rtf.LoadFromString(document.DocumentRange, rtf);

		Assert.That(loaded, Is.True);
		Assert.That(document.Text, Is.EqualTo("Some formatting text\nOaisjfoiajsdf aoisj foiasjd f asodijfoasidjf"));
		Assert.That(document.Count, Is.GreaterThanOrEqualTo(1));

		var runs = document.SelectMany(block => block.OfType<TextElement>()).ToList();
		Assert.That(runs.Count, Is.GreaterThanOrEqualTo(3));

		var helveticaRun = runs.FirstOrDefault(r => r.Text.Contains("Some formatting text", StringComparison.Ordinal));
		var arialRun = runs.FirstOrDefault(r => r.Text.Contains("aoisj foiasjd f", StringComparison.Ordinal));
		var zapfinoRun = runs.FirstOrDefault(r => r.Text.Contains("asodijfoasidjf", StringComparison.Ordinal));

		Assert.That(helveticaRun, Is.Not.Null);
		Assert.That(arialRun, Is.Not.Null);
		Assert.That(zapfinoRun, Is.Not.Null);

		Assert.That(helveticaRun!.Attributes?.Family?.Name, Is.EqualTo(familyName).IgnoreCase);
		Assert.That(arialRun!.Attributes?.Typeface?.PostScriptName, Is.EqualTo(firstPostScriptName).IgnoreCase);
		Assert.That(zapfinoRun!.Attributes?.Typeface?.PostScriptName, Is.EqualTo(secondPostScriptName).IgnoreCase);
	}

	[Test]
	public void RtfReaderShouldKeepStyledRunsOnSameLine()
	{
		var rtf = "{\\rtf1\\ansi\\ansicpg1252\\cocoartf2868\n"
			+ "\\cocoatextscaling0\\cocoaplatform0{\\fonttbl\\f0\\fswiss\\fcharset0 Helvetica;\\f1\\fswiss\\fcharset0 Helvetica-Bold;}\n"
			+ "{\\colortbl;\\red255\\green255\\blue255;}\n"
			+ "{\\*\\expandedcolortbl;;}\n"
			+ "\\pard\\tx720\\tx1440\\tx2160\\tx2880\\tx3600\\tx4320\\tx5040\\tx5760\\tx6480\\tx7200\\tx7920\\tx8640\\pardirnatural\\partightenfactor0\n"
			+ "\n"
			+ "\\f0\\fs24 \\cf0 Some formatting text\\\n"
			+ "\\\n"
			+ "\\\n"
			+ "Second line with \n"
			+ "\\f1\\b bold\n"
			+ "\\f0\\b0  text}";

		var document = new Document();
		var loaded = DocumentFormat.Rtf.LoadFromString(document.DocumentRange, rtf);

		Assert.That(loaded, Is.True);
		Assert.That(document.Count, Is.EqualTo(3));
		Assert.That(document[2].Text, Is.EqualTo("Second line with bold text"));
		Assert.That(document.Text, Is.EqualTo("Some formatting text\n\nSecond line with bold text"));
	}

	[Test]
	public void RtfReaderShouldParseCocoaParagraphBreaksAndMixedStyles()
	{
		var rtf = "{\\rtf1\\ansi\\ansicpg1252\\cocoartf2868\n"
			+ "\\cocoatextscaling0\\cocoaplatform0{\\fonttbl\\f0\\fswiss\\fcharset0 Helvetica;\\f1\\fswiss\\fcharset0 Helvetica-Bold;\\f2\\fswiss\\fcharset0 Helvetica-Oblique;\n"
			+ "\\f3\\fswiss\\fcharset0 Helvetica-BoldOblique;}\n"
			+ "{\\colortbl;\\red255\\green255\\blue255;\\red251\\green0\\blue7;}\n"
			+ "{\\*\\expandedcolortbl;;\\cssrgb\\c100000\\c12195\\c0;}\n"
			+ "\\pard\\tx720\\tx1440\\tx2160\\tx2880\\tx3600\\tx4320\\tx5040\\tx5760\\tx6480\\tx7200\\tx7920\\tx8640\\pardirnatural\\partightenfactor0\n"
			+ "\n"
			+ "\\f0\\fs24 \\cf0 Some \n"
			+ "\\f1\\b bold\n"
			+ "\\f0\\b0  text\\\n"
			+ "Some \n"
			+ "\\f2\\i italic \n"
			+ "\\f0\\i0 text\\\n"
			+ "Some \n"
			+ "\\f3\\i\\b \\cf2 bold and italic\\cf0  \n"
			+ "\\f0\\i0\\b0 text}";

		var document = new Document();
		var loaded = DocumentFormat.Rtf.LoadFromString(document.DocumentRange, rtf);

		Assert.That(loaded, Is.True);
		Assert.That(document.Count, Is.EqualTo(3));
		Assert.That(document.Text, Is.EqualTo("Some bold text\nSome italic text\nSome bold and italic text"));

		var paragraph1Runs = document[0].OfType<TextElement>().ToList();
		var paragraph2Runs = document[1].OfType<TextElement>().ToList();
		var paragraph3Runs = document[2].OfType<TextElement>().ToList();

		var boldRun = paragraph1Runs.FirstOrDefault(r => string.Equals(r.Text.Trim(), "bold", StringComparison.Ordinal));
		Assert.That(boldRun, Is.Not.Null);
		Assert.That(boldRun!.ActualAttributes.Bold, Is.True);

		var italicRun = paragraph2Runs.FirstOrDefault(r => r.Text.Contains("italic", StringComparison.Ordinal));
		Assert.That(italicRun, Is.Not.Null);
		Assert.That(italicRun!.ActualAttributes.Italic, Is.True);

		var redBoldItalicRun = paragraph3Runs.FirstOrDefault(r => r.Text.Contains("bold and italic", StringComparison.Ordinal));
		Assert.That(redBoldItalicRun, Is.Not.Null);
		Assert.That(redBoldItalicRun!.ActualAttributes.Bold, Is.True);
		Assert.That(redBoldItalicRun.ActualAttributes.Italic, Is.True);
		Assert.That(redBoldItalicRun.ActualAttributes.Foreground, Is.TypeOf<SolidBrush>());
		Assert.That(((SolidBrush)redBoldItalicRun.ActualAttributes.Foreground!).Color, Is.EqualTo(Color.FromArgb(251, 0, 7)));
	}

	[Test]
	public void RtfReaderShouldResolveBoldTypefaceFromPostScriptFontEntry()
	{
		var candidate = Fonts.AvailableFontFamilies
			.Select(family => new
			{
				Family = family,
				Regular = family.Typefaces.FirstOrDefault(tf => !tf.Bold && !tf.Italic && !string.IsNullOrWhiteSpace(tf.PostScriptName)),
				Bold = family.Typefaces.FirstOrDefault(tf => tf.Bold && !tf.Italic)
			})
			.FirstOrDefault(x => x.Regular != null && x.Bold != null);

		if (candidate is null)
			Assert.Inconclusive("No family with regular+bold faces and a postscript regular name was available.");

		var regularPostScriptName = candidate!.Regular!.PostScriptName!;
		var rtf = "{\\rtf1\\ansi\\ansicpg1252\\deff0"
			+ "{\\fonttbl{\\f0\\fnil " + regularPostScriptName + ";}}"
			+ "\\pard\\f0 Some \\b bold\\b0  text\\par}";

		var document = new Document();
		var loaded = DocumentFormat.Rtf.LoadFromString(document.DocumentRange, rtf);

		Assert.That(loaded, Is.True);
		Assert.That(document.Text, Is.EqualTo("Some bold text"));

		var runs = document.SelectMany(block => block.OfType<TextElement>()).ToList();
		var boldRun = runs.FirstOrDefault(r => string.Equals(r.Text.Trim(), "bold", StringComparison.Ordinal));
		Assert.That(boldRun, Is.Not.Null);
		Assert.That(boldRun!.ActualAttributes.Bold, Is.True);
		Assert.That(boldRun.ActualAttributes.Typeface, Is.Not.Null);
		Assert.That(boldRun.ActualAttributes.Typeface!.Bold, Is.True);
		Assert.That(boldRun.ActualAttributes.Font.Bold, Is.True);
		Assert.That(boldRun.ActualAttributes.Family?.Name, Is.EqualTo(candidate!.Family.Name).IgnoreCase);
	}

	[Test]
	public void RtfWriterShouldSkipDefaultForegroundColor()
	{
		var document = new Document();
		document.DefaultForeground = new SolidBrush(Colors.White);
		document.InsertText(0, "Hello");

		var rtf = DocumentFormat.Rtf.SaveToString(document.DocumentRange);

		Assert.That(rtf, Does.Not.Contain(@"\cf1"));
		Assert.That(rtf, Does.Not.Contain(@"\red255\green255\blue255"));
	}

	[Test]
	public void RtfRoundtripShouldPreserveLeadingTabs()
	{
		var source = new Document();
		source.InsertText(0, "\ttry\n\t{\n\t\tvar value = 1;\n\t}");

		var serialized = DocumentFormat.Rtf.SaveToString(source.DocumentRange);
		var destination = new Document();
		var loaded = DocumentFormat.Rtf.LoadFromString(destination.DocumentRange, serialized);

		Assert.That(loaded, Is.True);
		Assert.That(destination.Text, Is.EqualTo("\ttry\n\t{\n\t\tvar value = 1;\n\t}"));
		Assert.That(destination.Count, Is.EqualTo(4));
		Assert.That(destination[0].Text, Is.EqualTo("\ttry"));
		Assert.That(destination[1].Text, Is.EqualTo("\t{"));
		Assert.That(destination[2].Text, Is.EqualTo("\t\tvar value = 1;"));
		Assert.That(destination[3].Text, Is.EqualTo("\t}"));
	}

	[Test]
	public void RtfReaderShouldPreserveTabIndentedParagraphs()
	{
		var rtf = "{\\rtf1\\ansi\\ansicpg1252\\deff0"
			+ "{\\fonttbl{\\f0 Menlo;}}"
			+ "{\\colortbl;\\red204\\green204\\blue204;}"
			+ "\\pard\\plain\\f0\\fs24\\cf1 \\tab try\\par"
			+ "\\pard\\plain\\f0\\fs24\\cf1 \\tab \\{\\par"
			+ "\\pard\\plain\\f0\\fs24\\cf1 \\tab\\tab var value = 1;\\par"
			+ "\\pard\\plain\\f0\\fs24\\cf1 \\tab \\}\\par"
			+ "}";

		var document = new Document();
		var loaded = DocumentFormat.Rtf.LoadFromString(document.DocumentRange, rtf);

		Assert.That(loaded, Is.True);
		Assert.That(document.Count, Is.EqualTo(4));
		Assert.That(document.Text, Is.EqualTo("\ttry\n\t{\n\t\tvar value = 1;\n\t}"));
	}

	[Test]
	public void RtfReaderShouldParseWordStyleParagraphsColorAndItalics()
	{
		var colorTableEntries = string.Concat(Enumerable.Repeat("\\red0\\green0\\blue0;", 18)) + "\\red238\\green0\\blue0;";
		var rtf = "{\\rtf1\\ansi\\ansicpg1252\\deff0"
			+ "{\\fonttbl{\\f31506 Aptos;}}"
			+ "{\\colortbl;" + colorTableEntries + "}"
			+ "\\pard\\plain "
			+ "{\\rtlch\\fcs1 \\af31507\\afs24 \\ltrch\\fcs0 \\fs24\\loch\\f31506 Some }"
			+ "{\\rtlch\\fcs1 \\ab\\af31507 \\ltrch\\fcs0 \\b\\loch\\f31506 bold}"
			+ "{\\rtlch\\fcs1 \\af31507 \\ltrch\\fcs0 \\loch\\f31506  text\\par \\loch\\f31506 Some }"
			+ "{\\rtlch\\fcs1 \\ai\\af31507 \\ltrch\\fcs0 \\i\\loch\\f31506 italic}"
			+ "{\\rtlch\\fcs1 \\af31507 \\ltrch\\fcs0 \\loch\\f31506  text\\par \\loch\\f31506 Some }"
			+ "{\\rtlch\\fcs1 \\ab\\ai\\af31507 \\ltrch\\fcs0 \\b\\i\\cf19\\loch\\f31506 bold, Red, and Italic}"
			+ "{\\rtlch\\fcs1 \\af31507 \\ltrch\\fcs0 \\cf19\\loch\\f31506  }"
			+ "{\\rtlch\\fcs1 \\af31507 \\ltrch\\fcs0 \\loch\\f31506 text\\par}"
			+ "}";

		var document = new Document();
		var loaded = DocumentFormat.Rtf.LoadFromString(document.DocumentRange, rtf);

		Assert.That(loaded, Is.True);
		Assert.That(document.Text, Is.EqualTo("Some bold text\nSome italic text\nSome bold, Red, and Italic text"));
		Assert.That(document.Count, Is.EqualTo(3));

		var runs = document.SelectMany(block => block.OfType<TextElement>()).ToList();

		var italicRun = runs.FirstOrDefault(r => string.Equals(r.Text.Trim(), "italic", StringComparison.OrdinalIgnoreCase));
		Assert.That(italicRun, Is.Not.Null);
		Assert.That(italicRun!.ActualAttributes.Italic, Is.True);

		var redBoldItalicRun = runs.FirstOrDefault(r => r.Text.Contains("bold, Red, and Italic", StringComparison.Ordinal));
		Assert.That(redBoldItalicRun, Is.Not.Null);
		Assert.That(redBoldItalicRun!.ActualAttributes.Bold, Is.True);
		Assert.That(redBoldItalicRun.ActualAttributes.Italic, Is.True);
		Assert.That(redBoldItalicRun.ActualAttributes.Font.Bold, Is.True);
		Assert.That(redBoldItalicRun.ActualAttributes.Font.Italic, Is.True);
		Assert.That(redBoldItalicRun.ActualAttributes.Foreground, Is.TypeOf<SolidBrush>());
		Assert.That(((SolidBrush)redBoldItalicRun.ActualAttributes.Foreground!).Color, Is.EqualTo(Color.Parse("#EE0000")));
	}
}
