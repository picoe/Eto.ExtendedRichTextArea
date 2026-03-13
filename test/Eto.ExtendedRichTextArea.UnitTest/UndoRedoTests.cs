using Eto.Drawing;
using Eto.ExtendedRichTextArea.Model;

namespace Eto.ExtendedRichTextArea.UnitTest;

public class UndoRedoTests : TestBase
{
	static DocumentState CreateState(Document document) => new DocumentState(document);

	// ── basic state ──────────────────────────────────────────────────────────

	[Test]
	public void InitialStateShouldHaveNoUndoOrRedo()
	{
		var document = new Document();
		var state = CreateState(document);
		Assert.That(state.CanUndo, Is.False);
		Assert.That(state.CanRedo, Is.False);
	}

	[Test]
	public void UndoOnEmptyStackShouldReturnFalse()
	{
		var document = new Document();
		var state = CreateState(document);
		Assert.That(state.Undo(), Is.False);
	}

	[Test]
	public void RedoOnEmptyStackShouldReturnFalse()
	{
		var document = new Document();
		var state = CreateState(document);
		Assert.That(state.Redo(), Is.False);
	}

	// ── single edit ──────────────────────────────────────────────────────────

	[Test]
	public void SingleInsertShouldBeUndoable()
	{
		var document = new Document();
		var state = CreateState(document);

		document.InsertText(0, "Hello");

		Assert.That(state.CanUndo, Is.True);
		Assert.That(state.CanRedo, Is.False);
	}

	[Test]
	public void UndoAfterSingleInsertShouldRestoreEmptyDocument()
	{
		var document = new Document();
		var state = CreateState(document);

		document.InsertText(0, "Hello");
		Assert.That(state.Undo(), Is.True);

		Assert.That(document.Text, Is.EqualTo(""));
		Assert.That(state.CanUndo, Is.False);
		Assert.That(state.CanRedo, Is.True);
	}

	[Test]
	public void RedoAfterUndoShouldRestoreInsertedText()
	{
		var document = new Document();
		var state = CreateState(document);

		document.InsertText(0, "Hello");
		state.Undo();
		Assert.That(state.Redo(), Is.True);

		Assert.That(document.Text, Is.EqualTo("Hello"));
		Assert.That(state.CanUndo, Is.True);
		Assert.That(state.CanRedo, Is.False);
	}

	// ── multiple edits ───────────────────────────────────────────────────────

	[Test]
	public void MultipleUndosShouldWalkBackThroughEachEdit()
	{
		var document = new Document();
		var state = CreateState(document);

		document.InsertText(0, "Hello");   // snap ""
		document.InsertText(5, " World"); // snap "Hello"
		document.InsertText(11, "!");      // snap "Hello World"

		Assert.That(document.Text, Is.EqualTo("Hello World!"));

		state.Undo();
		Assert.That(document.Text, Is.EqualTo("Hello World"));

		state.Undo();
		Assert.That(document.Text, Is.EqualTo("Hello"));

		state.Undo();
		Assert.That(document.Text, Is.EqualTo(""));

		Assert.That(state.CanUndo, Is.False);
	}

	[Test]
	public void MultipleRedosShouldWalkForwardThroughEachEdit()
	{
		var document = new Document();
		var state = CreateState(document);

		document.InsertText(0, "Hello");
		document.InsertText(5, " World");
		document.InsertText(11, "!");

		state.Undo();
		state.Undo();
		state.Undo();

		state.Redo();
		Assert.That(document.Text, Is.EqualTo("Hello"));

		state.Redo();
		Assert.That(document.Text, Is.EqualTo("Hello World"));

		state.Redo();
		Assert.That(document.Text, Is.EqualTo("Hello World!"));

		Assert.That(state.CanRedo, Is.False);
	}

	[Test]
	public void UndoThenRedoThenUndoAgainShouldBeCorrect()
	{
		var document = new Document();
		var state = CreateState(document);

		document.InsertText(0, "A"); // snap ""
		document.InsertText(1, "B"); // snap "A"
		document.InsertText(2, "C"); // snap "AB"

		// undo back to "A"
		state.Undo(); // restores "AB"
		state.Undo(); // restores "A"
		Assert.That(document.Text, Is.EqualTo("A"));

		// redo to "AB"
		state.Redo();
		Assert.That(document.Text, Is.EqualTo("AB"));

		// undo again should go back to "A", not "ABC"
		state.Undo();
		Assert.That(document.Text, Is.EqualTo("A"));
	}

	// ── new edit clears redo stack ───────────────────────────────────────────

	[Test]
	public void NewEditAfterUndoShouldClearRedoStack()
	{
		var document = new Document();
		var state = CreateState(document);

		document.InsertText(0, "Hello");
		state.Undo();
		Assert.That(state.CanRedo, Is.True);

		// new edit — redo stack should be gone
		document.InsertText(0, "World");
		Assert.That(state.CanRedo, Is.False);
		Assert.That(document.Text, Is.EqualTo("World"));
	}

	// ── attribute changes ────────────────────────────────────────────────────

	[Test]
	public void UndoAfterAttributeChangeShouldRestoreOriginalAttributes()
	{
		var document = new Document();
		var state = CreateState(document);

		document.InsertText(0, "Hello");
		var boldAttributes = new Attributes { Bold = true };
		document.SetAttributes(0, 5, boldAttributes);

		// attributes are bold — undo should remove bold
		state.Undo();
		var attrs = document.GetAttributes(0, 5);
		Assert.That(attrs?.Bold, Is.Not.True);
	}

	[Test]
	public void RedoAfterUndoingAttributeChangeShouldReapplyAttributes()
	{
		var document = new Document();
		var state = CreateState(document);

		document.InsertText(0, "Hello");
		var boldAttributes = new Attributes { Bold = true };
		document.SetAttributes(0, 5, boldAttributes);

		state.Undo(); // undo bold
		state.Redo(); // redo bold

		var attrs = document.GetAttributes(0, 5);
		Assert.That(attrs?.Bold, Is.True);
	}

	// ── max capacity ─────────────────────────────────────────────────────────

	[Test]
	public void UndoStackShouldNotExceedMaxCapacity()
	{
		var document = new Document();
		var state = new DocumentState(document, maxUndoRedoStackSize: 3);

		document.InsertText(0, "A"); // snap ""
		document.InsertText(1, "B"); // snap "A"
		document.InsertText(2, "C"); // snap "AB"
		document.InsertText(3, "D"); // snap "ABC" — oldest ("") dropped

		// can undo exactly 3 times
		Assert.That(state.Undo(), Is.True);
		Assert.That(state.Undo(), Is.True);
		Assert.That(state.Undo(), Is.True);
		Assert.That(state.Undo(), Is.False);

		// oldest snapshot was "A" (the "" was dropped)
		Assert.That(document.Text, Is.EqualTo("A"));
	}

	// ── clear ────────────────────────────────────────────────────────────────

	[Test]
	public void ClearShouldRemoveAllHistory()
	{
		var document = new Document();
		var state = CreateState(document);

		document.InsertText(0, "Hello");
		document.InsertText(5, " World");
		state.Clear();

		Assert.That(state.CanUndo, Is.False);
		Assert.That(state.CanRedo, Is.False);
		Assert.That(state.Undo(), Is.False);
	}

	// ── multiline ────────────────────────────────────────────────────────────

	[Test]
	public void UndoAfterMultilineInsertShouldRestoreDocument()
	{
		var document = new Document();
		var state = CreateState(document);

		document.InsertText(0, "Hello\nWorld\nFoo");
		Assert.That(document.Count, Is.EqualTo(3));

		state.Undo();
		Assert.That(document.Text, Is.EqualTo(""));
		Assert.That(document.Count, Is.EqualTo(0));
	}

	[Test]
	public void UndoAfterDeleteShouldRestoreDeletedText()
	{
		var document = new Document();
		var state = CreateState(document);

		document.InsertText(0, "Hello World");
		document.RemoveAt(5, 6); // remove " World"

		Assert.That(document.Text, Is.EqualTo("Hello"));

		state.Undo();
		Assert.That(document.Text, Is.EqualTo("Hello World"));
	}
}
