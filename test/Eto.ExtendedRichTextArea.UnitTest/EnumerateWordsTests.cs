using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Eto.ExtendedRichTextArea.Model;

using NUnit.Framework;

namespace Eto.ExtendedRichTextArea.UnitTest;

[TestFixture]
public class EnumerateWordsTests : TestBase
{
	[TestCase("Hello there", 0, true, "0:Hello;6:there")]
	[TestCase("Hello there", 5, true, "0:Hello;6:there")]
	[TestCase("Hello there", 6, true, "6:there")]
	[TestCase("Hello there", 7, true, "6:there")]
	[TestCase("Hello there\nfriends", 6, true, "6:there;11:;12:friends")]
	[TestCase("Hello there\nfriends", 11, true, "11:;12:friends")]
	[TestCase("Hello there\nfriends of Eto", 12, true, "12:friends;20:of;23:Eto")]
	[TestCase("Hello there\nfriends of Eto", 15, true, "12:friends;20:of;23:Eto")]
	[TestCase("Hello there\nfriends of Eto", 19, true, "12:friends;20:of;23:Eto")]
	[TestCase("Hello there\nfriends of Eto", 20, true, "20:of;23:Eto")]

	[TestCase("Hello there", 0, false, "")]
	[TestCase("Hello there", 5, false, "0:Hello")]
	[TestCase("Hello there", 6, false, "6:there;0:Hello")]
	[TestCase("Hello there", 7, false, "6:there;0:Hello")]
	[TestCase("Hello there\nfriends", 6, false, "6:there;0:Hello")]
	[TestCase("Hello there\nfriends", 12, false, "11:;6:there;0:Hello")]
	[TestCase("Hello there\nfriends of Eto", 15, false, "12:friends;11:;6:there;0:Hello")]
	[TestCase("Hello there\nfriends of Eto", 19, false, "12:friends;11:;6:there;0:Hello")]
	[TestCase("Hello there\nfriends of Eto", 20, false, "20:of;12:friends;11:;6:there;0:Hello")]
	public void EnumerateWordsShouldWork(string text, int index, bool forward, string expected)
	{
		var document = new Document();
		document.Text = text;

		var words = document.EnumerateWords(index, forward).ToList();
		var result = string.Join(";", words.Select(r => $"{r.start}:{r.text}"));
		Assert.That(result, Is.EqualTo(expected));
	}
}