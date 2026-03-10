using Eto.Forms;
using Eto.Drawing;
using System;
using Eto.ExtendedRichTextArea.Model;
using System.ComponentModel;

namespace Eto.ExtendedRichTextArea;

public class ExtendedRichTextArea : Scrollable
{
	readonly TextAreaDrawable _drawable;
	IEnumerable<IElement>? _selectionElements;
	Attributes? _selectionAttributes;
	Attributes? _lastSelectionAttributes;

	/// <summary>
	/// Gets or sets the document to display and edit in the rich text area.
	/// </summary>
	public Document Document
	{
		get => _drawable.Document;
		set => _drawable.Document = value;
	}
	
	/// <summary>
	/// Gets or sets the placeholder document to show when the main document is empty.
	/// This can be used to show a hint to the user of what to enter in the document.
	/// </summary>
	public Document? Placeholder
	{
		get => _drawable.Placeholder;
		set => _drawable.Placeholder = value;
	}
	
	/// <summary>
	/// Gets or sets a value indicating whether the rich text area is read-only.
	/// </summary>
	public bool ReadOnly
	{
		get => _drawable.ReadOnly;
		set => _drawable.ReadOnly = value;
	}

	/// <summary>
	/// Gets or sets a value indicating whether the selection should always be shown, even when the control does not have focus.
	/// </summary>
	public bool AlwaysShowSelection
	{
		get => _drawable.AlwaysShowSelection;
		set => _drawable.AlwaysShowSelection = value;
	}

	/// <summary>
	/// Gets or sets the attributes to apply to the current selection or caret position.
	/// </summary>
	public Attributes SelectionAttributes
	{
		get
		{
			if (_selectionAttributes != null)
				_selectionAttributes.PropertyChanged -= SelectionAttributes_PropertyChanged;
			_selectionAttributes ??= Document.DefaultAttributes.Clone();
			if (_selectionAttributes != null)
				_selectionAttributes.PropertyChanged += SelectionAttributes_PropertyChanged;
			return _selectionAttributes!;
		}
		set
		{
			if (_selectionAttributes != null)
				_selectionAttributes.PropertyChanged -= SelectionAttributes_PropertyChanged;
			_selectionAttributes = value;
			// needs to be cleared otherwise applying 'bold' for example
			// a 2nd time doesn't work.
			_lastSelectionAttributes = null;
			if (_selectionAttributes != null)
				_selectionAttributes.PropertyChanged += SelectionAttributes_PropertyChanged;
			SelectionAttributesChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	private void SelectionAttributes_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (_selectionAttributes == null)
			return;
		if (e.PropertyName == nameof(Attributes.Font)) // not actually a property we apply
			return;
			
		if (_lastSelectionAttributes != null && _selectionAttributes.Equals(_lastSelectionAttributes))
			return;
		_lastSelectionAttributes = _selectionAttributes.Clone();

		UpdateSelectionAttributes();
	}

	private void UpdateSelectionAttributes()
	{
		if (_selectionAttributes != null)
			_drawable.Selection.Attributes = _selectionAttributes;
	}

	/// <summary>
	/// Event raised when the selection attributes have changed, either by setting the SelectionAttributes property or by changing a property on the current SelectionAttributes.
	/// </summary>
	public event EventHandler<EventArgs>? SelectionAttributesChanged;

	/// <summary>
	/// Event raised when the selection has changed, either by setting the Selection property or by the user changing the selection. This is also raised when the caret position changes and there is no selection, so that the SelectionAttributes can be updated to match the new caret position.
	/// </summary>
	public event EventHandler<EventArgs>? SelectionChanged;

	/// <summary>
	/// Gets or sets the current selection in the document. Setting this will also update the SelectionAttributes to match the attributes of the new selection.
	/// </summary>
	public DocumentRange Selection
	{
		get => _drawable.Selection;
		set => _drawable.SetSelection(value, true);
	}

	/// <summary>
	/// Gets or sets the text of the current selection in the document.
	/// </summary>
	public string SelectionText
	{
		get => _drawable.Selection?.Text ?? string.Empty;
		set => Selection.Text = value;
	}

	/// <summary>
	/// Raises the SelectionChanged event. This should be called whenever the selection changes, either by setting the Selection property or by the user changing the selection.
	/// </summary>
	/// <param name="e">The event arguments.</param>
	protected virtual void OnSelectionChanged(EventArgs e)
	{
		_selectionElements = null;
		SelectionChanged?.Invoke(this, e);
		SelectionElementsChanged?.Invoke(this, e);
	}

	internal Font SelectionFont
	{
		get => _selectionAttributes?.Font ?? Document.DefaultFont;
	}

	/// <summary>
	/// Event raised when the caret index has changed, either by setting the CaretIndex property or by the user moving the caret.
	/// </summary>
	public event EventHandler<EventArgs>? CaretIndexChanged;
	
	/// <summary>
	/// Raises the CaretIndexChanged event. This is called whenever the caret index changes, either by setting the CaretIndex property or by the user moving the caret.
	/// </summary>
	/// <param name="e">The event arguments.</param>
	protected virtual void OnCaretIndexChanged(EventArgs e)
	{
		CaretIndexChanged?.Invoke(this, EventArgs.Empty);
		CaretElementChanged?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>
	/// Gets or sets the index of the caret in the document. Setting this will also clear any selection.
	/// </summary>
	public int CaretIndex
	{
		get => _drawable.Caret.Index;
		set
		{
			_drawable.Caret.SetIndex(value, false);
			_drawable.SetSelection(null, true);
		}
	}

	/// <summary>
	/// Event raised when the element at the caret position has changed, either by moving the caret or by changing the document. This can be used to update UI to show the attributes of the current element at the caret position.
	/// </summary>
	public event EventHandler<EventArgs>? CaretElementChanged;
	
	/// <summary>
	/// Gets the element at the current caret position. This can be used to get the attributes of the current element at the caret position, or to determine if the caret is inside a specific type of element.
	/// </summary>
	public IElement? CaretElement => Document.FindElementAt(CaretIndex);

	/// <summary>
	/// Event raised when the elements in the current selection have changed, either by changing the selection or by changing the document. This can be used to update UI to show the attributes of the current selection.
	/// </summary>
	public event EventHandler<EventArgs>? SelectionElementsChanged;


	/// <summary>
	/// Gets or sets the elements in the current selection. Setting this will replace the current selection with the new elements.
	/// </summary>
	public IEnumerable<IElement> SelectionElements
	{
		get => _selectionElements ??= Document.Enumerate(Selection.Start, Selection.End, false, true).ToList();
		set => Document.Replace(Selection.Start, Selection.Length, value);
	}

	/// <summary>
	/// Sets the focus to the rich text area, allowing the user to interact with it. This will also show the caret and selection if applicable. If the rich text area is inside a scrollable container, it will also scroll to make sure the caret is visible.
	/// </summary>
	public override void Focus()
	{
		_drawable.Focus();
	}
	
	/// <summary>
	/// Gets or sets the context menu to show when the user right-clicks on the rich text area. This can be used to provide a custom context menu with options specific to the rich text area, such as cut/copy/paste, formatting options, etc.
	/// </summary>
	public new ContextMenu ContextMenu
	{
		get => _drawable.ContextMenu;
		set => _drawable.ContextMenu = value;
	}

	/// <summary>
	/// Gets a value indicating whether the rich text area currently has focus. This is true if the user has clicked inside the control or has tabbed to it, and false if the user has clicked outside the control or has tabbed away from it.
	/// </summary>
	public new bool HasFocus => _drawable.HasFocus;

	/// <summary>
	/// Initializes a new instance of the ExtendedRichTextArea class. This is a rich text area control that supports editing and formatting of text, as well as inserting images and other elements. It also supports undo/redo, copy/paste, and other common text editing features.
	/// </summary>
	public ExtendedRichTextArea()
	{
		_drawable = new TextAreaDrawable(this);
		_drawable.Caret.IndexChanged += Drawable_CaretIndexChanged;
		_drawable.SelectionChanged += Drawable_SelectionChanged;
		_drawable.GotFocus += (s, e) => OnGotFocus(e);
		_drawable.LostFocus += (s, e) => OnLostFocus(e);

		Content = _drawable;

		BackgroundColor = SystemColors.ControlBackground;
		Size = new Size(200, 100);
	}

	private void Drawable_SelectionChanged(object? sender, EventArgs e)
	{
		OnSelectionChanged(e);
	}

	protected override void OnLoadComplete(EventArgs e)
	{
		base.OnLoadComplete(e);
		UpdateAvailableSize();
	}

	protected override void OnSizeChanged(EventArgs e)
	{
		base.OnSizeChanged(e);
		UpdateAvailableSize();
	}

	private void UpdateAvailableSize()
	{
		if (_drawable != null)
			_drawable.SetAvailableSize(ClientSize);
	}

	private void Drawable_CaretIndexChanged(object? sender, EventArgs e)
	{
		var scrollSize = ClientSize;
		PointF scrollPosition = ScrollPosition;
		var caretBounds = _drawable.CaretBounds;

		if (caretBounds.Bottom > scrollPosition.Y + scrollSize.Height)
		{
			scrollPosition.Y = caretBounds.Bottom - scrollSize.Height;
		}
		else if (caretBounds.Top < scrollPosition.Y)
		{
			scrollPosition.Y = caretBounds.Top;
		}

		if (caretBounds.Right > scrollPosition.X + scrollSize.Width)
		{
			scrollPosition.X = caretBounds.Right - scrollSize.Width;
		}
		else if (caretBounds.Left < scrollPosition.X)
		{
			scrollPosition.X = caretBounds.Left;
		}

		ScrollPosition = Point.Round(scrollPosition);
		OnSelectionChanged(EventArgs.Empty);
		OnCaretIndexChanged(EventArgs.Empty);
	}

	/// <summary>
	/// Inserts the specified text at the current caret position, replacing any selected text.
	/// The caret will be moved to the end of the inserted text.
	/// </summary>
	/// <param name="text">The text to insert.</param>
	public void InsertText(string text)
	{
		Document.BeginEdit();
		if (_drawable.Selection?.Length > 0)
		{
			Document.RemoveAt(_drawable.Selection.Start, _drawable.Selection.Length);
			_drawable.Caret.SetIndex(_drawable.Selection.Start, false);
			_drawable.SetSelection(null, false);
		}
		Document.InsertText(_drawable.Caret.Index, text, SelectionAttributes);
		Document.EndEdit();
		_drawable.Caret.SetIndex(_drawable.Caret.Index + text.Length, false);
		_drawable.SetSelection(null, false);
		Invalidate();
	}

	/// <summary>
	/// Inserts the specified element at the current caret position, replacing any selected text. The caret will be moved to the end of the inserted element. 
	/// This can be used to insert images, tables, or other non-text elements into the document.
	/// </summary>
	/// <param name="element">The element to insert.</param>
	public void Insert(IElement element)
	{
		Document.BeginEdit();
		if (_drawable.HasSelection)
		{
			Document.RemoveAt(_drawable.Selection.Start, _drawable.Selection.Length);
			_drawable.SetSelection(null, false);
		}
		Document.InsertAt(_drawable.Caret.Index, element);
		Document.EndEdit();
		_drawable.Caret.SetIndex(element.DocumentStart + element.Length, true);
		Invalidate();
	}

}
