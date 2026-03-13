using Eto.ExtendedRichTextArea.Model;

namespace Eto.ExtendedRichTextArea;

/// <summary>
/// Manages undo/redo history for a <see cref="Document"/>.
/// Create a new instance for each document you want to track.
/// </summary>
public class DocumentState
{
	readonly Document _document;
	bool _isPerformingUndoRedo;
	readonly FixedSizeStack<DocumentSnapshot> _undoStack;
	readonly FixedSizeStack<DocumentSnapshot> _redoStack;

	/// <summary>
	/// Optional delegate to capture extra state (e.g., caret position, scroll position)
	/// at the time a snapshot is taken. The captured value is passed to
	/// <see cref="RestoreExtra"/> and <see cref="RestoreExtraPost"/> during undo/redo.
	/// </summary>
	public Func<object?>? CaptureExtra { get; set; }

	/// <summary>
	/// Optional delegate called inside <c>Document.BeginEdit()</c>/<c>EndEdit()</c>
	/// during a restore (e.g., to restore caret position and selection).
	/// Receives the value previously returned by <see cref="CaptureExtra"/>.
	/// </summary>
	public Action<object?>? RestoreExtra { get; set; }

	/// <summary>
	/// Optional delegate called after <c>Document.EndEdit()</c> during a restore
	/// (e.g., to restore scroll position after layout has been finalized).
	/// Receives the value previously returned by <see cref="CaptureExtra"/>.
	/// </summary>
	public Action<object?>? RestoreExtraPost { get; set; }

	public bool CanUndo => _undoStack.Count > 0;
	public bool CanRedo => _redoStack.Count > 0;

	public DocumentState(Document document, int maxUndoRedoStackSize = 100)
	{
		_document = document;
		_document.Changing += OnDocumentChanging;
		_undoStack = new FixedSizeStack<DocumentSnapshot>(maxUndoRedoStackSize);
		_redoStack = new FixedSizeStack<DocumentSnapshot>(maxUndoRedoStackSize);
	}

	private void OnDocumentChanging(object? sender, EventArgs e) => SaveState();

	/// <summary>
	/// Manually captures a snapshot of the current document state and clears the redo stack.
	/// Normally called automatically via the <c>Document.Changing</c> event.
	/// </summary>
	public void SaveState()
	{
		if (_isPerformingUndoRedo)
			return;
		_undoStack.Push(Capture());
		_redoStack.Clear();
	}

	DocumentSnapshot Capture() =>
		new DocumentSnapshot(
			_document.Select(e => (IBlockElement)e.Clone()).ToList(),
			_document.Attributes?.Clone(),
			CaptureExtra?.Invoke());

	void RestoreSnapshot(DocumentSnapshot snapshot)
	{
		_document.BeginEdit();
		_document.Attributes = snapshot.DocumentAttributes;
		_document.Clear();
		_document.AddRange(snapshot.Elements);
		RestoreExtra?.Invoke(snapshot.ExtraState);
		_document.EndEdit();
		RestoreExtraPost?.Invoke(snapshot.ExtraState);
	}

	/// <summary>
	/// Undoes the last change. Returns <c>true</c> if a state was restored.
	/// </summary>
	public bool Undo()
	{
		if (!CanUndo)
			return false;
		_isPerformingUndoRedo = true;
		var current = Capture();
		var snapshot = _undoStack.Pop();
		RestoreSnapshot(snapshot);
		_redoStack.Push(current);
		_isPerformingUndoRedo = false;
		return true;
	}

	/// <summary>
	/// Redoes the last undone change. Returns <c>true</c> if a state was restored.
	/// </summary>
	public bool Redo()
	{
		if (!CanRedo)
			return false;
		_isPerformingUndoRedo = true;
		var current = Capture();
		var snapshot = _redoStack.Pop();
		RestoreSnapshot(snapshot);
		_undoStack.Push(current);
		_isPerformingUndoRedo = false;
		return true;
	}

	/// <summary>
	/// Clears all undo and redo history.
	/// </summary>
	public void Clear()
	{
		_undoStack.Clear();
		_redoStack.Clear();
		_isPerformingUndoRedo = false;
	}

	readonly struct DocumentSnapshot
	{
		internal DocumentSnapshot(List<IBlockElement> elements, Attributes? documentAttributes, object? extraState)
		{
			Elements = elements;
			DocumentAttributes = documentAttributes;
			ExtraState = extraState;
		}

		internal List<IBlockElement> Elements { get; }
		internal Attributes? DocumentAttributes { get; }
		internal object? ExtraState { get; }
	}
}
