using System;

using Eto.Drawing;

namespace Eto.ExtendedRichTextArea.UnitTest;

[TestFixture]
public class GetAttributeTests : TestBase
{

	[Test]
	public void SelectedRangeWithDifferentAttributesShouldHaveNullAttributes()
	{
		var document = new Document
		{
			new ParagraphElement
			{
				new TextElement { Text = "Hello " },
				new TextElement { Text = "World", Attributes = new Attributes { Family = FontFamilies.Serif } }
			}
		};

		Attributes attributes;

		attributes = document.GetAttributes(6, 6);
		Assert.That(attributes.Family, Is.Not.Null);
		Assert.That(attributes.Family.Name, Is.EqualTo(document.DefaultAttributes.Family?.Name));
		Assert.That(attributes.Typeface, Is.Not.Null);
		Assert.That(attributes.Bold, Is.EqualTo(document.DefaultAttributes.Bold));
		Assert.That(attributes.Italic, Is.EqualTo(document.DefaultAttributes.Italic));

		attributes = document.GetAttributes(0, 11);
		Assert.That(attributes.Family, Is.Null);
		Assert.That(attributes.Typeface, Is.Null);
		
		attributes = document.GetAttributes(0, 5);
		Assert.That(attributes.Family, Is.Not.Null);
		Assert.That(attributes.Family.Name, Is.EqualTo(document.DefaultAttributes.Family?.Name));
		Assert.That(attributes.Typeface, Is.Not.Null);

		attributes = document.GetAttributes(0, 0);
		Assert.That(attributes.Family, Is.Not.Null);
		Assert.That(attributes.Family.Name, Is.EqualTo(document.DefaultAttributes.Family?.Name));
		Assert.That(attributes.Typeface, Is.Not.Null);


		attributes = document.GetAttributes(7, 7);
		Assert.That(attributes.Family, Is.Not.Null);
		Assert.That(attributes.Family.Name, Is.EqualTo(FontFamilies.Serif.Name));
		Assert.That(attributes.Typeface, Is.Not.Null);

		attributes = document.GetAttributes(6, 11);
		Assert.That(attributes.Family, Is.Not.Null);
		Assert.That(attributes.Family.Name, Is.EqualTo(FontFamilies.Serif.Name));
		Assert.That(attributes.Typeface, Is.Not.Null);
	}
}
