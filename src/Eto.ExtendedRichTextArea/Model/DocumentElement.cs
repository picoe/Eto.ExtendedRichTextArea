using Eto.Drawing;

using System.Collections;

namespace Eto.ExtendedRichTextArea.Model
{
	public interface IDocumentElement
	{
		int Start { get; set; }
		int Length { get; }
		int End { get; }
		int DocumentIndex { get; }
		RectangleF Bounds { get; }
		string Text { get; internal set; }
		IDocumentElement? Parent { get; set; }
		IDocumentElement? Split(int index);
		SizeF Measure(SizeF availableSize, PointF location);
		int Remove(int index, int length);
		void Recalculate(int index);
		int GetIndexAtPoint(PointF point);
		PointF? GetPointAtIndex(int index);
		IEnumerable<(string text, int index)> EnumerateWords(int start, bool forward);
	}
	
	public abstract class DocumentElement<T> : IEnumerable<T>, IDocumentElement
		where T : class, IDocumentElement, new()
	{
		readonly List<T> _elements = new List<T>();
		
		public int Start { get; private set; }
		public int Length { get; private set; }
		public int End => Start + Length;
		public RectangleF Bounds { get; private set; }
		public int DocumentIndex => Start + Parent?.DocumentIndex ?? 0;
		
		
		protected IDocumentElement? Parent { get; private set; }
		
		protected IDocumentElement? TopParent
		{
			get
			{
				var parent = Parent;
				while (parent?.Parent != null)
				{
					parent = parent.Parent;
				}
				return parent;
			}
		}
		
		IDocumentElement? IDocumentElement.Parent
		{
			get => Parent;
			set => Parent = value;
		}

		protected IList<T> Children => _elements;

		protected virtual string? Separator => null;

		protected abstract void OffsetElement(ref PointF location, ref SizeF size, SizeF elementSize);

		int IDocumentElement.Start
		{
			get => Start;
			set => Start = value;
		}

		public string Text
		{
			get
			{
				if (Separator != null)
					return string.Join(Separator, _elements.Select(r => r.Text));
				return string.Concat(_elements.Select(r => r.Text));
			}
			set
			{
				_elements.Clear();
				if (!string.IsNullOrEmpty(value))
				{
					if (Separator != null)
					{
						var lines = value.Split(new[] { Separator }, StringSplitOptions.None);
						foreach (var line in lines)
						{
							var element = new T();
							element.Text = line;
							element.Parent = this;
							_elements.Add(element);
						}
					}
					else
					{
						var element = new T();
						element.Text = value;
						element.Parent = this;
						_elements.Add(element);
					}
				}
				MeasureIfNeeded();
			}
		}

		internal abstract DocumentElement<T> Create();

		public IEnumerator<T> GetEnumerator() => _elements.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public SizeF Measure(SizeF availableSize, PointF location)
		{
			var size = MeasureOverride(availableSize, location);
			Bounds = new RectangleF(location, size);
			return size;
		}

		protected virtual SizeF MeasureOverride(SizeF availableSize, PointF location)
		{
			SizeF size = SizeF.Empty;
			int index = 0;
			var separatorLength = Separator?.Length ?? 0;
			PointF elementLocation = location;
			for (int i = 0; i < _elements.Count; i++)
			{
				if (i > 0)
					index += separatorLength;
				var element = _elements[i];
				element.Start = index;
				var elementSize = element.Measure(availableSize, elementLocation);

				OffsetElement(ref elementLocation, ref size, elementSize);
				index += element.Length;
			}
			Length = index;
			return size;
		}

		internal T? Find(int position)
		{
			for (int i = 0; i < _elements.Count; i++)
			{
				var element = _elements[i];
				if (position <= element.Start + element.Length)
					return element;
			}
			return null;
		}

		public IEnumerable<(string text, int index)> EnumerateWords(int start, bool forward)
		{
			if (forward)
			{
				for (int i = 0; i < _elements.Count; i++)
				{
					var element = _elements[i];
					foreach (var word in element.EnumerateWords(start, forward))
					{
						yield return (word.text, word.index + element.Start);
					}
				}
			}
			else
			{
				for (int i = _elements.Count - 1; i >= 0; i--)
				{
					var element = _elements[i];
					foreach (var word in element.EnumerateWords(start, forward))
					{
						yield return (word.text, word.index + element.Start);
					}
				}
			}
		}

		public void Insert(int index, T element)
		{
			element.Start = index;
			element.Parent = this;
			for (int i = 0; i < _elements.Count; i++)
			{
				if (_elements[i].Start >= index)
				{
					_elements.Insert(i, element);
					Adjust(i, element.Length);
					MeasureIfNeeded();
					return;
				}
			}
			_elements.Add(element);
			Length += element.Length;
			MeasureIfNeeded();
		}

		private void Adjust(int startIndex, int length)
		{
			Length += length;
			for (int j = startIndex + 1; j < _elements.Count; j++)
			{
				_elements[j].Start += length;
			}
		}

		public virtual void Add(T element)
		{
			element.Start = Length;
			element.Parent = this;
			_elements.Add(element);
			Length += element.Length;
			MeasureIfNeeded();
		}

		public virtual void Remove(T element)
		{
			var index = _elements.IndexOf(element);
			if (index == -1)
				return;
			_elements.Remove(element);
			Adjust(index, -element.Length);
			MeasureIfNeeded();
		}

		public int Remove(int index, int length)
		{
			var originalLength = length;
			for (int i = 0; i < _elements.Count; i++)
			{
				// if we've removed all the characters, we're done
				if (length <= 0)
					break;
					
				var element = _elements[i];
				var start = index;
				var end = start + length;
				if (element.Start > end || element.End < start)
					continue;
				if (start <= element.Start && end >= element.End)
				{
					Remove(element);
					length -= element.Length;
					continue;
				}

				if (start < element.Start && end >= element.End)
				{
					var removeLength = element.Length - start;
					length -= element.Remove(start - element.Start, removeLength);
				}
				else if (start >= element.Start && end < element.End)
				{
					length -= element.Remove(start - element.Start, length);
				}
				else if (end > element.End && element is Paragraph paragraph)
				{
					// merge the next paragraph into this one
					if (i + 1 < _elements.Count)
					{
						var nextElement = _elements[i + 1];
						if (nextElement is Paragraph nextParagraph)
						{
							// merge first row of next paragraph into last row of this paragraph
							var nextRow = nextParagraph.Children.FirstOrDefault();
							if (nextRow != null)
							{
								var row = paragraph.Children.LastOrDefault();
								if (row != null)
								{
									nextParagraph.Remove(nextRow);
									foreach (var span in nextRow)
									{
										row.Add(span);
									}
								}
							}
							// add the remaining rows to this paragraph (if any)
							foreach (var row in nextParagraph)
							{
								paragraph.Add(row);
							}
							_elements.Remove(nextElement);
							length--; // newline
						}
					}
				}
				else
				{
					length -= element.Remove(start - element.Start, length);
				}
				if (element.Length == 0)
				{
					Remove(element);
				}
				if (length > 0)
				{
					Recalculate(Start);
				}

			}
			MeasureIfNeeded();
			return originalLength - length;
		}

		internal virtual void MeasureIfNeeded()
		{
		}

		IDocumentElement? IDocumentElement.Split(int index)
		{
			if (index >= Length || Start == index)
				return null;
			DocumentElement<T> CreateNew()
			{
				var newElement = Create();
				newElement.Parent = Parent;
				return newElement;
			}

			DocumentElement<T>? newRun = null;
			for (int i = 0; i < _elements.Count; i++)
			{
				var element = _elements[i];
				if (element.Start >= index)
				{
					_elements.Remove(element);
					i--;
					newRun ??= CreateNew();
					element.Start = newRun.Length;
					newRun.Add(element);
				}
				else if (element.End > index)
				{
					newRun ??= CreateNew();
					if (element.Split(index - element.Start) is T newElement)
					{
						newElement.Start = newRun.Length;
						newRun.Add(newElement);
					}
				}
			}
			return newRun;
		}

		void IDocumentElement.Recalculate(int index) => Recalculate(index);
		internal void Recalculate(int index)
		{
			Start = index;
			for (int i = 0; i < _elements.Count; i++)
			{
				if (i > 0)
					index++; // newline
				var element = _elements[i];
				element.Recalculate(index);
				index += element.Length;
			}
			Length = index;
		}
		
		public PointF? GetPointAtIndex(int index)
		{
			var element = Find(index);
			var point = element?.GetPointAtIndex(index - element.Start);
			return point ?? Bounds.Location;
		}
		
        public int GetIndexAtPoint(PointF point)
        {
			if (point.Y < Bounds.Top)
				return 0;
			if (point.Y > Bounds.Bottom)
				return Length;
			for (int i= 0; i < _elements.Count; i++)
			{
				var element = _elements[i];

				// too far, break!
				if (point.Y < element.Bounds.Top)
					break;
				if (point.Y > element.Bounds.Bottom)
					continue;

				var index = element.GetIndexAtPoint(point);
				if (index >= 0)
					return index + element.Start;
			}
			return Length;
        }
		
	}
}
