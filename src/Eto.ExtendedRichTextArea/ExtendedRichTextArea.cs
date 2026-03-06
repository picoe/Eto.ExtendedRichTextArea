using Eto.Forms;
using Eto.Drawing;
using System;
using Eto.ExtendedRichTextArea.Model;
using System.ComponentModel;

namespace Eto.ExtendedRichTextArea;

public class ExtendedRichTextArea : Scrollable
{
	readonly TextAreaDrawable _drawable;

	public Document Document
	{
		get => _drawable.Document;
		set => _drawable.Document = value;
	}
	
	public bool ReadOnly
	{
		get => _drawable.ReadOnly;
		set => _drawable.ReadOnly = value;
	}

	public bool AlwaysShowSelection
	{
		get => _drawable.AlwaysShowSelection;
		set => _drawable.AlwaysShowSelection = value;
	}

	Attributes? _selectionAttributes;
	Attributes? _lastSelectionAttributes;

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

	public event EventHandler<EventArgs>? SelectionAttributesChanged;

	public DocumentRange Selection
	{
		get => _drawable.Selection;
		set => _drawable.SetSelection(value, true);
	}
	
	public string SelectionText
	{
		get => _drawable.Selection?.Text ?? string.Empty;
		set => Selection.Text = value;
	}

	public event EventHandler<EventArgs>? SelectionChanged;
	
	protected virtual void OnSelectionChanged(EventArgs e)
	{
		_selectionElements = null;
		SelectionChanged?.Invoke(this, e);
		SelectionElementsChanged?.Invoke(this, e);
	}

	public Font SelectionFont
	{
		get => _selectionAttributes?.Font ?? Document.DefaultFont;
		set
		{
			_selectionAttributes ??= new Attributes();
			_selectionAttributes.Font = value;
			SelectionFontChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	public event EventHandler<EventArgs>? SelectionFontChanged;


	public Brush SelectionBrush
	{
		get => _selectionAttributes?.Foreground ?? Document.DefaultForeground;
		set
		{
			_selectionAttributes ??= new Attributes();
			_selectionAttributes.Foreground = value;
			SelectionBrushChanged?.Invoke(this, EventArgs.Empty);
		}
	}
	
	public event EventHandler<EventArgs>? CaretIndexChanged;
	
	protected virtual void OnCaretIndexChanged(EventArgs e)
	{
		CaretIndexChanged?.Invoke(this, EventArgs.Empty);
		CaretElementChanged?.Invoke(this, EventArgs.Empty);
	}

	public int CaretIndex
	{
		get => _drawable.Caret.Index;
		set
		{
			_drawable.Caret.SetIndex(value, false);
			_drawable.SetSelection(null, true);
		}
	}

	public event EventHandler<EventArgs>? CaretElementChanged;

	public IElement? CaretElement => Document.FindElementAt(CaretIndex);

	public event EventHandler<EventArgs>? SelectionElementsChanged;


	IEnumerable<IElement>? _selectionElements;
	public IEnumerable<IElement> SelectionElements
	{
		get => _selectionElements ??= Document.Enumerate(Selection.Start, Selection.End, false, true).ToList();
		set => Document.Replace(Selection.Start, Selection.Length, value);
	}


	public event EventHandler<EventArgs>? SelectionBrushChanged;

	public override void Focus()
	{
		_drawable.Focus();
	}
	
	public new ContextMenu ContextMenu
	{
		get => _drawable.ContextMenu;
		set => _drawable.ContextMenu = value;
	}

	public new bool HasFocus => _drawable.HasFocus;

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
			_drawable.Document.AvailableSize = ClientSize;
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
