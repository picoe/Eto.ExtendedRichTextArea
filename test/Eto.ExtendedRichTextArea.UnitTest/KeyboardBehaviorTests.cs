using System.Reflection;
using Eto.Forms;

namespace Eto.ExtendedRichTextArea.UnitTest;

public class KeyboardBehaviorTests : TestBase
{
	static object GetPrivateField(object instance, string name)
	{
		var field = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.That(field, Is.Not.Null, $"Expected private field '{name}' on {instance.GetType().Name}.");
		return field!.GetValue(instance)!;
	}

	static void InvokeManipulationKey(ExtendedRichTextArea textArea, Keys key)
	{
		var drawable = GetPrivateField(textArea, "_drawable");
		var keyboard = GetPrivateField(drawable, "_keyboard");
		var method = keyboard.GetType().GetMethod("TextArea_KeyDown_Manipulation", BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.That(method, Is.Not.Null, "Expected TextArea_KeyDown_Manipulation method.");
		method!.Invoke(keyboard, new object[] { new KeyEventArgs(key, KeyEventType.KeyDown) });
	}

	static void InvokeKeyDown(ExtendedRichTextArea textArea, Keys key)
	{
		var drawable = GetPrivateField(textArea, "_drawable");
		var keyboard = GetPrivateField(drawable, "_keyboard");
		var method = keyboard.GetType().GetMethod("TextArea_KeyDown", BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.That(method, Is.Not.Null, "Expected TextArea_KeyDown method.");
		method!.Invoke(keyboard, new object?[] { drawable, new KeyEventArgs(key, KeyEventType.KeyDown) });
	}

	// The platform's word-navigation modifier: Option on macOS, Ctrl elsewhere.
	static Keys WordModifier => Platform.Instance.IsMac ? Keys.Alt : Keys.Control;

	[Test]
	public void ExtendSelectionByPreviousWordShouldGrowOneWordPerPress()
	{
		var textArea = new ExtendedRichTextArea();
		textArea.Document.Text = "<> a few words";
		textArea.CaretIndex = textArea.Document.Length;

		var key = WordModifier | Keys.Shift | Keys.Left;
		var expected = new[] { "words", "few words", "a few words", "<> a few words" };
		foreach (var text in expected)
		{
			InvokeKeyDown(textArea, key);
			Assert.That(textArea.SelectionText, Is.EqualTo(text));
		}
	}

	[Test]
	public void ExtendSelectionByNextWordShouldGrowOneWordPerPress()
	{
		var textArea = new ExtendedRichTextArea();
		textArea.Document.Text = "<> a few words";
		textArea.CaretIndex = 0;

		var key = WordModifier | Keys.Shift | Keys.Right;
		var expected = new[] { "<> ", "<> a ", "<> a few ", "<> a few words" };
		foreach (var text in expected)
		{
			InvokeKeyDown(textArea, key);
			Assert.That(textArea.SelectionText, Is.EqualTo(text));
		}
	}

	[Test]
	public void MoveToPreviousWordShouldNotSelect()
	{
		var textArea = new ExtendedRichTextArea();
		textArea.Document.Text = "<> a few words";
		textArea.CaretIndex = textArea.Document.Length;

		InvokeKeyDown(textArea, WordModifier | Keys.Left);

		Assert.That(textArea.CaretIndex, Is.EqualTo(9));
		Assert.That(textArea.SelectionText, Is.Empty);
	}

	[Test]
	public void DeleteWordBackwardShouldDeletePreviousWord()
	{
		var textArea = new ExtendedRichTextArea();
		textArea.Document.Text = "<> a few words";
		textArea.CaretIndex = textArea.Document.Length;

		var expected = new[] { "<> a few ", "<> a ", "<> ", "" };
		foreach (var text in expected)
		{
			InvokeKeyDown(textArea, WordModifier | Keys.Backspace);
			Assert.That(textArea.Document.Text, Is.EqualTo(text));
			Assert.That(textArea.CaretIndex, Is.EqualTo(text.Length));
		}
	}

	[Test]
	public void DeleteWordBackwardInsideWordShouldDeleteToWordStart()
	{
		var textArea = new ExtendedRichTextArea();
		textArea.Document.Text = "<> a few words";
		textArea.CaretIndex = 7;

		InvokeKeyDown(textArea, WordModifier | Keys.Backspace);

		Assert.That(textArea.Document.Text, Is.EqualTo("<> a w words"));
		Assert.That(textArea.CaretIndex, Is.EqualTo(5));
	}

	[Test]
	public void DeleteWordForwardShouldDeleteNextWord()
	{
		var textArea = new ExtendedRichTextArea();
		textArea.Document.Text = "<> a few words";
		textArea.CaretIndex = 0;

		var expected = new[] { "a few words", "few words", "words", "" };
		foreach (var text in expected)
		{
			InvokeKeyDown(textArea, WordModifier | Keys.Delete);
			Assert.That(textArea.Document.Text, Is.EqualTo(text));
			Assert.That(textArea.CaretIndex, Is.EqualTo(0));
		}
	}

	[TestCase(Keys.Backspace)]
	[TestCase(Keys.Delete)]
	public void DeleteWordWithSelectionShouldDeleteOnlySelection(Keys key)
	{
		var textArea = new ExtendedRichTextArea();
		textArea.Document.Text = "<> a few words";
		textArea.Selection = textArea.Document.GetRange(5, 8);

		InvokeKeyDown(textArea, WordModifier | key);

		Assert.That(textArea.Document.Text, Is.EqualTo("<> a  words"));
		Assert.That(textArea.CaretIndex, Is.EqualTo(5));
	}

	[Test]
	public void BackspaceShouldDeleteEntireEmojiSurrogatePair()
	{
		var textArea = new ExtendedRichTextArea();
		textArea.Document.Text = "A😀B";
		textArea.CaretIndex = 3;

		InvokeManipulationKey(textArea, Keys.Backspace);

		Assert.That(textArea.Document.Text, Is.EqualTo("AB"));
		Assert.That(textArea.CaretIndex, Is.EqualTo(1));
	}

	[Test]
	public void DeleteShouldDeleteEntireEmojiSurrogatePair()
	{
		var textArea = new ExtendedRichTextArea();
		textArea.Document.Text = "A😀B";
		textArea.CaretIndex = 1;

		InvokeManipulationKey(textArea, Keys.Delete);

		Assert.That(textArea.Document.Text, Is.EqualTo("AB"));
		Assert.That(textArea.CaretIndex, Is.EqualTo(1));
	}
}
