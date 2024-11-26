using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Eto.ExtendedRichTextArea.Model;

namespace Eto.ExtendedRichTextArea.UnitTest;

public class NavigationTests : TestBase
{
	[TestCase("Hello\nWorld", 0, DocumentNavigationMode.NextLine, 6)]
	[TestCase("Hello\nThere\nWorld", 0, DocumentNavigationMode.NextLine, 6)]
	[TestCase("Hello\nThere\nWorld", 6, DocumentNavigationMode.NextLine, 12)]
	[TestCase("Hello\n\nWorld", 6, DocumentNavigationMode.NextLine, 7)]
	[TestCase("Hello\nand\nWorld", 5, DocumentNavigationMode.NextLine, 9)]

	
	[TestCase("Hello\nWorld", 6, DocumentNavigationMode.PreviousLine, 0)]
	[TestCase("Hello\n\nWorld", 7, DocumentNavigationMode.PreviousLine, 6)]
	[TestCase("Hello\nand\nWorld", 14, DocumentNavigationMode.PreviousLine, 9)]
	
	[TestCase("Hello\nWorld", 2, DocumentNavigationMode.EndOfLine, 5)]
	[TestCase("Hello\nWorld", 0, DocumentNavigationMode.EndOfLine, 5)]
	[TestCase("Hello\nWorld", 6, DocumentNavigationMode.EndOfLine, 11)]
	[TestCase("Hello\nWorld", 8, DocumentNavigationMode.EndOfLine, 11)]
	[TestCase("Hello\nWorld", 11, DocumentNavigationMode.EndOfLine, 11)]
	public void NavigateShouldWork(string text, int start, DocumentNavigationMode mode, int expected)
	{
		var document = new Document();
		document.Text = text;
		var result = document.Navigate(start, mode);
		Assert.That(result, Is.EqualTo(expected));
	}
}
