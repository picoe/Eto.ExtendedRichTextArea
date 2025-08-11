using Eto.Forms;
using Eto.Drawing;
using System;
using Eto.ExtendedRichTextArea.Model;
using System.ComponentModel;

namespace Eto.ExtendedRichTextArea
{
    public class ExtendedRichTextArea : Scrollable
	{
		readonly TextAreaDrawable _drawable;
		
		public Document Document
		{
			get => _drawable.Document;
			set => _drawable.Document = value;
		}
		
		Attributes? _selectionAttributes;
		
		public Attributes? SelectionAttributes
		{
			get
			{
				if (_selectionAttributes != null)
					_selectionAttributes.PropertyChanged -= SelectionAttributes_PropertyChanged;
				_selectionAttributes ??= Document.DefaultAttributes.Clone();
				if (_selectionAttributes != null)
					_selectionAttributes.PropertyChanged += SelectionAttributes_PropertyChanged;
				return _selectionAttributes;
			}
			set
			{
				if (_selectionAttributes != null)
					_selectionAttributes.PropertyChanged -= SelectionAttributes_PropertyChanged;
				_selectionAttributes = value;
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

			UpdateSelectionAttributes();
		}

		private void UpdateSelectionAttributes()
		{
			_drawable.Selection?.SetAttributes(_selectionAttributes);
		}

		public event EventHandler<EventArgs>? SelectionAttributesChanged;

		public DocumentRange Selection
		{
			get => _drawable.Selection ??= Document.GetRange(_drawable.Caret.Index, _drawable.Caret.Index);
			set => _drawable.Selection = value;
		}

		 public string SelectionText
		 {
		 	get => _drawable.Selection?.Text ?? string.Empty;
		 	set => Selection.Text = value;
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

		public event EventHandler<EventArgs>? SelectionBrushChanged;

        public override void Focus()
        {
            _drawable.Focus();
        }

        public ExtendedRichTextArea()
		{
			_drawable = new TextAreaDrawable(this);
			_drawable.Caret.IndexChanged += Drawable_CaretIndexChanged;
			
			Content = _drawable;

			BackgroundColor = SystemColors.ControlBackground;
			Size = new Size(200, 100);
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			// When wrapping is enabled, we need to specify how much space we have
			// It's not working just yet.
			// if (_drawable != null)
			// 	_drawable.Document.AvailableSize = ClientSize;
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
        }

		public void InsertText(string text)
		{
			Document.BeginEdit();
			if (_drawable.Selection?.Length > 0)
			{
				Document.RemoveAt(_drawable.Selection.Start, _drawable.Selection.Length);
				_drawable.Caret.Index = _drawable.Selection.Start;
				_drawable.Selection = null;
			}
			Document.InsertText(_drawable.Caret.Index, text, SelectionAttributes);
			Document.EndEdit();
			_drawable.Caret.Index += text.Length;
			_drawable.Caret.CalculateCaretBounds();
			Invalidate();
		}
		
		public void Insert(IElement element)
		{
			Document.BeginEdit();
			if (_drawable.Selection != null)
			{
				Document.RemoveAt(_drawable.Selection.Start, _drawable.Selection.Length);
				_drawable.Selection = null;
			}
			Document.InsertAt(_drawable.Caret.Index, element);
			Document.EndEdit();
			_drawable.Caret.Index = element.End;
			_drawable.Caret.CalculateCaretBounds();
			Invalidate();
		}
	}
}
