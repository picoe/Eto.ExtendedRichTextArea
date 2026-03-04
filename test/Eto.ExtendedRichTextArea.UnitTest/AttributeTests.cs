using Eto.Drawing;
using Eto.ExtendedRichTextArea.Model;

namespace Eto.ExtendedRichTextArea.UnitTest;

[TestFixture]
public class AttributeTests : TestBase
{
	[Test]
	public void MergingAttributesWithFontSetShouldWork()
	{
		var defaultFamily = CourierNewFontFamily;

		var font1 = new Font(defaultFamily, 12, FontStyle.Bold);
		var font2 = new Font(defaultFamily, 16, FontStyle.Italic);

		var attributes1 = new Attributes { Font = font1 };
		var attributes2 = new Attributes { Font = font2 };

		// Verify both attributes decomposed their fonts correctly
		Assert.That(attributes1.Bold, Is.True);
		Assert.That(attributes1.Italic, Is.Not.True);
		Assert.That(attributes1.Size, Is.EqualTo(12f));

		Assert.That(attributes2.Bold, Is.Not.True);
		Assert.That(attributes2.Italic, Is.True);
		Assert.That(attributes2.Size, Is.EqualTo(16f));

		// Merge attributes2 onto attributes1 — second should override first
		var merged = attributes1.Merge(attributes2, copy: true);

		Assert.That(merged.Font, Is.Not.Null, "Merged font should not be null");
		Assert.That(merged.Family, Is.EqualTo(defaultFamily), "Family should be preserved");
		Assert.That(merged.Size, Is.EqualTo(16f), "Size should come from the overriding attributes");
		Assert.That(merged.Bold, Is.False, "Bold should come from the overriding attributes");
		Assert.That(merged.Italic, Is.True, "Italic should come from the overriding attributes");
	}
	
	[Test]
	public void MergingAttributesWithDifferentFontFamiliesShouldWork()
	{
		var defaultFamily = CourierNewFontFamily;
		var otherFamily = TimesNewRomanFontFamily;

		var font1 = new Font(defaultFamily, 12, FontStyle.None);
		var font2 = new Font(otherFamily, 16, FontStyle.None);

		var attributes1 = new Attributes { Font = font1 };
		var attributes2 = new Attributes { Font = font2 };

		var merged = attributes1.Merge(attributes2, copy: true);

		Assert.That(merged.Family, Is.EqualTo(otherFamily), "Family should come from the overriding attributes");
		Assert.That(merged.Typeface, Is.Not.Null, "Typeface should not be null");
		Assert.That(merged.Size, Is.EqualTo(16f), "Size should come from the overriding attributes");
		Assert.That(merged.Bold, Is.False, "Bold should come from the overriding attributes");
		Assert.That(merged.Italic, Is.False, "Italic should come from the overriding attributes");
		Assert.That(merged.Font, Is.Not.Null, "Merged font should not be null");
	}
	
}