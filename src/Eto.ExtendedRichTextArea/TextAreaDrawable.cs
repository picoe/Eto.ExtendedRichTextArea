using Eto.Forms;
using Eto.Drawing;
using System;
using Eto.ExtendedRichTextArea.Model;
using Eto.ExtendedRichTextArea.Commands;

namespace Eto.ExtendedRichTextArea
{

	class TextAreaDrawable : Drawable
	{
		const int MaxUndoRedoStackSize = 100;
		Document? _document;
		readonly CaretBehavior _caret;
		readonly KeyboardBehavior _keyboard;
		readonly MouseBehavior _mouse;
#if DEBUG
		bool _isValid = true;
#endif
		Scrollable? _parentScrollable;
		DocumentRange? _selection;
		readonly ExtendedRichTextArea _textArea;
		bool _isPerformingUndoRedo;

		public Document Document
		{
			get => _document ?? (Document = new Document());
			set
			{
				if (_document != null)
				{
					_document.Changed -= Document_Changed;
					_document.OverrideAttributes -= Document_OverrideAttributes;
					_document.DefaultAttributesChanged -= Document_DefaultAttributesChanged;
					_document.Changing -= Document_BeginEditEvent;
				}
				_document = value ?? new Document();
				_document.Changed += Document_Changed;
				_document.OverrideAttributes += Document_OverrideAttributes;
				_document.DefaultAttributesChanged += Document_DefaultAttributesChanged;
				_document.Changing += Document_BeginEditEvent;
				_caret.Index = 0;
				_caret.CalculateCaretBounds();
				Invalidate(false);
				ClearUndoRedoStacks();
			}
		}

		private void Document_BeginEditEvent(object? sender, EventArgs e)
		{
			SaveState();
		}

		private void Document_DefaultAttributesChanged(object? sender, EventArgs e)
		{
			if (Selection?.Length > 0)
				_textArea.SelectionAttributes = Document.GetAttributes(Selection.Start, Selection.End);
			else
				_textArea.SelectionAttributes = Document.GetAttributes(_caret.Index, _caret.Index);
		}

		public Attributes HighlightAttributes { get; set; } = new Attributes { Background = new SolidBrush(SystemColors.Highlight), Foreground = new SolidBrush(SystemColors.HighlightText) };

		private void Document_OverrideAttributes(object? sender, OverrideAttributesEventArgs e)
		{
			if (Selection?.Length > 0)
				e.NewAttributes.Add(new AttributeRange(Selection.Start, Selection.End, HighlightAttributes));
		}

		private void Document_Changed(object? sender, EventArgs e)
		{
			_caret.CalculateCaretBounds();
			Size = Size.Ceiling(Document.Size);
#if DEBUG
			_isValid = Document.IsValid();
#endif
			Invalidate(false);

			// Shouldn't be needed after https://github.com/picoe/Eto/pull/2709
			if (Platform.IsWpf)
				_parentScrollable?.UpdateScrollSizes();
		}

		internal CaretBehavior Caret => _caret;
		internal KeyboardBehavior Keyboard => _keyboard;
		internal MouseBehavior Mouse => _mouse;
		internal ExtendedRichTextArea TextArea => _textArea;

		public TextAreaDrawable(ExtendedRichTextArea textArea) : base(false)
		{
			_textArea = textArea;
			_caret = new CaretBehavior(this);
			_keyboard = new KeyboardBehavior(this, _caret);
			_mouse = new MouseBehavior(this, _caret);
			CanFocus = true;
		}

		public DocumentRange? Selection
		{
			get => _selection;
			set
			{
				_selection = value;
				if (_selection != null)
				{
					if (!ReferenceEquals(_selection.Document, Document))
						throw new ArgumentOutOfRangeException(nameof(value), "Selection must be from this document");

					_textArea.SelectionAttributes = Document.GetAttributes(_selection.Start, _selection.End);
				}
				else
					_textArea.SelectionAttributes = Document.GetAttributes(_caret.Index, _caret.Index);
				SelectionChanged?.Invoke(this, EventArgs.Empty);

				Invalidate(false);
			}
		}

		public event EventHandler<EventArgs>? SelectionChanged;



		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			if (Parent is Scrollable scrollable)
			{
				_parentScrollable = scrollable;
				_parentScrollable.Scroll += parentScrollable_Scroll;
			}
		}

		protected override void OnUnLoad(EventArgs e)
		{
			base.OnUnLoad(e);
			if (_parentScrollable != null)
			{
				_parentScrollable.Scroll -= parentScrollable_Scroll;
				_parentScrollable = null;
			}
		}

		private void parentScrollable_Scroll(object? sender, ScrollEventArgs e)
		{
			Invalidate(false);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

#if DEBUG
			if (!_isValid)
			{
				using var invalidText = new FormattedText { Text = "INVALID", Font = SystemFonts.Bold(), ForegroundBrush = Brushes.Red };
				var size = invalidText.Measure();
				var point = new PointF(Width - size.Width, 0);
				e.Graphics.DrawText(invalidText, point);
			}
#endif
			var clip = e.ClipRectangle;
			if (_parentScrollable != null && Loaded)
			{
				try
				{
					var rect = _parentScrollable.RectangleToScreen(new RectangleF(_parentScrollable.ClientSize));
					clip.Intersect(RectangleFromScreen(rect));
				}
				catch
				{
					// Eto.Wpf currently has an issue getting client size before loaded during a drawing operation, so ignore for now.
				}
			}
			// _selection?.Paint(e.Graphics);

			var screen = ParentWindow?.Screen ?? Screen.PrimaryScreen;
			Document.ScreenScale = screen.Scale;
			Document.Paint(e.Graphics, clip);
			_caret.Paint(e);
		}

		internal float Scale => ParentWindow?.Screen.Scale ?? 1;

		public RectangleF CaretBounds => _caret.CaretBounds;

		FixedSizeStack<DocumentState> UndoStack { get; } = new(MaxUndoRedoStackSize);

		FixedSizeStack<DocumentState> RedoStack { get; } = new(MaxUndoRedoStackSize);

		public bool CanUndo => UndoStack.Count > 0;

		public bool CanRedo => RedoStack.Count > 0;

		public bool Undo()
		{
			if (CanUndo)
			{
				_isPerformingUndoRedo = true;
				var state = UndoStack.Pop();
				state.Restore(this);
				RedoStack.Push(state);
				_isPerformingUndoRedo = false;
				Invalidate(false);
				return true;
			}
			return false;
		}

		public bool Redo()
		{
			if (CanRedo)
			{
				_isPerformingUndoRedo = true;
				var state = RedoStack.Pop();
				state.Restore(this);
				UndoStack.Push(state);
				_isPerformingUndoRedo = false;
				Invalidate(false);
				return true;
			}
			return false;
		}

		void SaveState()
		{
			if (_isPerformingUndoRedo)
				return;

			var state = new DocumentState(this);
			UndoStack.Push(state);
			RedoStack.Clear();
		}

		private void ClearUndoRedoStacks()
		{
			UndoStack.Clear();
			RedoStack.Clear();
			_isPerformingUndoRedo = false;
		}
		
		internal void SetSelection(int lastCaretIndex, bool extendSelection, bool useOriginalStart = true)
		{
			if (lastCaretIndex == _caret.Index)
				return;
				
			if (extendSelection)
			{
				if (Selection != null)
				{
					if (useOriginalStart)
						lastCaretIndex = Selection.OriginalStart;
					else
						lastCaretIndex = _caret.Index < Selection.Start ? Selection.End : Selection.Start;
				}
				Selection = Document.GetRange(lastCaretIndex, _caret.Index);
			}
			else
			{
				Selection = null; // Document.GetRange(_caret.Index, _caret.Index);
			}
		}
		
	}
}
