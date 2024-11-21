using Eto.Forms;
using Eto.Drawing;
using System;

namespace Eto.ExtendedRichTextArea
{
    public class NewRichTextArea : Scrollable
	{
		TextAreaDrawable _drawable;
		
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
		
		public event EventHandler<EventArgs> SelectionFontChanged;

        public override void Focus()
        {
            _drawable.Focus();
        }

        public NewRichTextArea()
		{
			Size = new Size(200, 100);
			_drawable = new TextAreaDrawable(this);
			_drawable.CaretIndexChanged += Drawable_CaretIndexChanged;
			_drawable.SelectionFontChanged += (sender, e) => SelectionFontChanged?.Invoke(this, e);
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
    }
}
