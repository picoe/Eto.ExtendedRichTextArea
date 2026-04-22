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
