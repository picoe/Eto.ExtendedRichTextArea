using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Eto.ExtendedRichTextArea.Model;

using NUnit.Framework;

namespace Eto.ExtendedRichTextArea.UnitTest;

public class RemoveTextTests : TestBase
{
	[TestCase("Hello\nWorld", 2, 1, "Helo\nWorld")]
	[TestCase("Hello\nWorld", 2, 3, "He\nWorld")]
	[TestCase("Hello\nWorld", 2, 4, "HeWorld")]
	[TestCase("Hello\nWorld", 2, 5, "Heorld")]
	[TestCase("Hello\nWorld", 2, 7, "Held")]
	[TestCase("Hello\nThere\nFun\nAnd\nExciting\nWorld", 8,11, "Hello\nTh\nExciting\nWorld")]
	[TestCase("Hello\nThere\nFun\nAnd\nExciting\nWorld", 8,12, "Hello\nThExciting\nWorld")]
	public void RemovingTextShouldWork(string initialText, int removeStart, int removeLength, string expected)
	{
		var document = new Document();
		document.Text = initialText;
		Assert.That(document.Text, Is.EqualTo(initialText)); // sanity
		
		document.RemoveAt(removeStart, removeLength);
		Assert.That(document.Text, Is.EqualTo(expected));
	}
}