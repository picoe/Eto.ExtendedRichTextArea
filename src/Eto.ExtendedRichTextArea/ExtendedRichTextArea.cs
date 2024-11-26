using Eto.Forms;
using Eto.Drawing;
using System;
using Eto.ExtendedRichTextArea.Model;

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
		
		public Font SelectionFont
		{
			get => _drawable.SelectionFont;
			set => _drawable.SelectionFont = value;
		}

		public Brush SelectionBrush
		{
			get => _drawable.SelectionBrush;
			set => _drawable.SelectionBrush = value;
		}
		
		public event EventHandler<EventArgs>? SelectionFontChanged;
		public event EventHandler<EventArgs>? SelectionBrushChanged;

        public override void Focus()
        {
            _drawable.Focus();
        }

        public ExtendedRichTextArea()
		{
			Size = new Size(200, 100);
			_drawable = new TextAreaDrawable(this);
			_drawable.CaretIndexChanged += Drawable_CaretIndexChanged;
			_drawable.SelectionFontChanged += (sender, e) => SelectionFontChanged?.Invoke(this, e);
			_drawable.SelectionBrushChanged += (sender, e) => SelectionBrushChanged?.Invoke(this, e);
			Content = _drawable;
			BackgroundColor = SystemColors.ControlBackground;
		}

        private void Drawable_CaretIndexChanged(object sender, EventArgs e)
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

		public void InsertText(string text) => _drawable.InsertText(text);

		public void Insert(IInlineElement imageElement) => _drawable.Insert(imageElement);
	}
}
