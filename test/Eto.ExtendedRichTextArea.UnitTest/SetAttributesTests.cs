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
		range.SetAttributes(attributes);

		var testAttributes = document.GetAttributes(testStart, testEnd);
		Assert.That(testAttributes?.Family?.Name, Is.EqualTo(testFamily));
	}
}
