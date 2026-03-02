using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Eto.Drawing;
using Eto.ExtendedRichTextArea.Model;

using NUnit.Framework;

namespace Eto.ExtendedRichTextArea.UnitTest;

public class SetAttributesTests : TestBase
{
	[TestCase("Hello\nWorld", 2, 8, "Courier New", "Arial", 0, 1)]
	[TestCase("Hello\nWorld", 2, 8, "Courier New", "Arial", 9, 10)]
	[TestCase("Hello\nWorld", 2, 8, "Courier New", "Courier New", 2, 4)]
	[TestCase("Hello\nWorld", 2, 8, "Courier New", "Courier New", 3, 3)]
	[TestCase("Hello\nWorld", 2, 8, "Courier New", "Courier New", 3, 5)]
	[TestCase("Hello\nWorld", 2, 8, "Courier New", "Courier New", 3, 8)]
	public void SettingFamilyShouldWork(string text, int start, int end, string family, string testFamily, int testStart, int testEnd)
	{
		var document = new Document();
		document.Text = text;

		var attributes = new Attributes { Family = new FontFamily(family) };
		var range = document.GetRange(start, end);
		range.Attributes = attributes;

		var testAttributes = document.GetAttributes(testStart, testEnd);
		Assert.That(testAttributes?.Family?.Name, Is.EqualTo(testFamily));
	}
	
	[Test]
	public void SettingBoldOnMixedRangeShouldWork()
	{
		var document = new Document
		{
			new ParagraphElement
			{
				new TextElement { Text = "Hello " },
				new TextElement { Text = "World", Attributes = new Attributes { Bold = true } }
			}
		};

		var attributes = document.GetAttributes(0, 11);
		Assert.That(attributes, Is.Not.Null);
		Assert.That(attributes.Bold, Is.Null);

		attributes.Bold = true;
		document.SetAttributes(0, 11, attributes);  //new Attributes { Bold = true });

		attributes = document.GetAttributes(0, 11);
		Assert.That(attributes, Is.Not.Null);
		Assert.That(attributes.Bold, Is.True);
		Assert.That(attributes.Font?.Bold, Is.True);
		
		Assert.That(document.Count, Is.EqualTo(1));
		Assert.That(((TextElement?)document[0][0])?.ActualAttributes?.Font?.FontStyle.HasFlag(FontStyle.Bold), Is.True);
		Assert.That(((TextElement?)document[0][1])?.ActualAttributes?.Font?.FontStyle.HasFlag(FontStyle.Bold), Is.True);
	}
}
