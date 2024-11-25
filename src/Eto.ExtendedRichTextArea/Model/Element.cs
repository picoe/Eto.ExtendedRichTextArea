using Eto.Drawing;
using Eto.ExtendedRichTextArea.Measure;

using System.Collections;
using System.Collections.ObjectModel;

namespace Eto.ExtendedRichTextArea.Model
{
	public interface IElement
	{
		int Start { get; set; }
		int Length { get; }
		int End { get; }
		int DocumentIndex { get; }
		RectangleF Bounds { get; }
		string Text { get; internal set; }
		IElement? Parent { get; set; }
		IElement? Split(int index);
		SizeF Measure(SizeF availableSize, PointF location);
		int RemoveAt(int index, int length);
		void Recalculate(int index);
		int GetIndexAtPoint(PointF point);
		PointF? GetPointAtIndex(int index);
		IEnumerable<(string text, int index)> EnumerateWords(int start, bool forward);
		IEnumerable<IInlineElement> EnumerateInlines(int start, int end);
		void MeasureIfNeeded();
		void Measure(Measurement measurement);
	}
	
	
	public abstract class Element<T> : Collection<T>, IElement
		where T : class, IElement
	{
		public int Start { get; private set; }
		public int Length { get; private set; }
		public int End => Start + Length;
		public RectangleF Bounds { get; private set; }
		public int DocumentIndex => Start + Parent?.DocumentIndex ?? 0;
		
		
		public IElement? Parent { get; private set; }
		
		protected static IElement? GetTopParent(IElement element)
		{
			var parent = element.Parent;
			while (parent?.Parent != null)
			{
				parent = parent.Parent;
			}
			return parent;
		}
		
		IElement? IElement.Parent
		{
			get => Parent;
			set => Parent = value;
		}

		protected virtual string? Separator => null;

		// protected abstract void OffsetElement(ref PointF location);

		protected abstract void OffsetElement(ref PointF location, ref SizeF size, SizeF elementSize);

		int IElement.Start
		{
			get => Start;
			set => Start = value;
		}

		public string Text
		{
			get
			{
				if (Separator != null)
					return string.Join(Separator, this.Select(r => r.Text));
				return string.Concat(this.Select(r => r.Text));
			}
			set
			{
				BeginEdit();
				Clear();
				if (!string.IsNullOrEmpty(value))
				{
					if (Separator != null)
					{
						var lines = value.Split(new[] { Separator }, StringSplitOptions.None);
						foreach (var line in lines)
						{
							var element = CreateElement();
							element.Text = line;
							element.Parent = this;
							Add(element);
						}
					}
					else
					{
						var element = CreateElement();
						element.Text = value;
						element.Parent = this;
						Add(element);
					}
				}
				EndEdit();
			}
		}

		protected override void ClearItems()
		{
			base.ClearItems();
			Length = 0;
			MeasureIfNeeded();
		}

		protected override void InsertItem(int index, T item)
		{
			base.InsertItem(index, item);
			Adjust(index, item.Length);
			MeasureIfNeeded();
		}

		protected override void RemoveItem(int index)
		{
			var element = this[index];
			base.RemoveItem(index);
			Adjust(index, -element.Length);
			MeasureIfNeeded();
		}

		protected override void SetItem(int index, T item)
		{
			var old = this[index];
			base.SetItem(index, item);
			Adjust(index, item.Length - old.Length);
			MeasureIfNeeded();
		}

		internal abstract Element<T> Create();
		internal abstract T CreateElement();

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
			for (int i = 0; i < Count; i++)
			{
				if (i > 0)
					index += separatorLength;
				var element = this[i];
				element.Start = index;
				var elementSize = element.Measure(availableSize, elementLocation);

				OffsetElement(ref elementLocation, ref size, elementSize);
				index += element.Length;
			}
			Length = index;
			return size;
		}
		
		public void Measure(Measurement measurement)
		{
			var location = measurement.CurrentLocation;
			if (this is Paragraph paragraph)
			{
				measurement.CurrentParagraph = paragraph;
				measurement.CurrentRun = null;
				measurement.CurrentLine = null;
			}
			else if (this is Run run)
			{
				measurement.AddNewLine(run);
			}
			var size = MeasureOverride(measurement);
			Bounds = new RectangleF(location, size);
		}

		protected virtual SizeF MeasureOverride(Measurement measurement)
		{
			SizeF size = SizeF.Empty;
			int index = 0;
			var separatorLength = Separator?.Length ?? 0;
			for (int i = 0; i < Count; i++)
			{
				if (i > 0)
					index += separatorLength;
				var element = this[i];
				element.Start = index;
				element.Measure(measurement);
				OffsetElement(measurement);
				index += element.Length;
			}
			Length = index;
			return size;
		}

		public virtual void OffsetElement(Measurement measurement)
		{
		}

		internal T? Find(int position)
		{
			for (int i = 0; i < Count; i++)
			{
				var element = this[i];
				if (position <= element.Start + element.Length)
					return element;
			}
			return null;
		}

		public IEnumerable<(string text, int index)> EnumerateWords(int start, bool forward)
		{
			if (forward)
			{
				for (int i = 0; i < Count; i++)
				{
					var element = this[i];
					foreach (var word in element.EnumerateWords(start, forward))
					{
						yield return (word.text, word.index + element.Start);
					}
				}
			}
			else
			{
				for (int i = Count - 1; i >= 0; i--)
				{
					var element = this[i];
					foreach (var word in element.EnumerateWords(start, forward))
					{
						yield return (word.text, word.index + element.Start);
					}
				}
			}
		}

		public void InsertAt(int index, T element)
		{
			element.Start = index;
			element.Parent = this;
			for (int i = 0; i < Count; i++)
			{
				if (this[i].Start >= index)
				{
					Insert(i, element);
					Adjust(i, element.Length);
					MeasureIfNeeded();
					return;
				}
			}
			Add(element);
			Length += element.Length;
			MeasureIfNeeded();
		}

		public void Adjust(int startIndex, int length)
		{
			Length += length;
			for (int j = startIndex + 1; j < Count; j++)
			{
				this[j].Start += length;
			}
		}

		public int RemoveAt(int index, int length)
		{
			var originalLength = length;
			for (int i = 0; i < Count; i++)
			{
				// if we've removed all the characters, we're done
				if (length <= 0)
					break;
					
				var element = this[i];
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
					length -= element.RemoveAt(start - element.Start, removeLength);
				}
				else if (start >= element.Start && end < element.End)
				{
					length -= element.RemoveAt(start - element.Start, length);
				}
				else if (end > element.End && element is Paragraph paragraph)
				{
					// merge the next paragraph into this one
					if (i + 1 < Count)
					{
						var nextElement = this[i + 1];
						if (nextElement is Paragraph nextParagraph)
						{
							// merge first row of next paragraph into last row of this paragraph
							var nextRow = nextParagraph.FirstOrDefault();
							if (nextRow != null)
							{
								var row = paragraph.LastOrDefault();
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
							Remove(nextElement);
							length--; // newline
						}
					}
				}
				else
				{
					length -= element.RemoveAt(start - element.Start, length);
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

		public virtual void BeginEdit()
		{
		}

		public virtual void EndEdit()
		{
		}

		internal virtual void MeasureIfNeeded() => Parent?.MeasureIfNeeded();

		void IElement.MeasureIfNeeded() => MeasureIfNeeded();

		IElement? IElement.Split(int index)
		{
			if (index >= Length || Start == index)
				return null;
			Element<T> CreateNew()
			{
				var newElement = Create();
				newElement.Parent = Parent;
				return newElement;
			}

			Element<T>? newRun = null;
			for (int i = 0; i < Count; i++)
			{
				var element = this[i];
				if (element.Start >= index)
				{
					Remove(element);
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

		void IElement.Recalculate(int index) => Recalculate(index);
		internal void Recalculate(int index)
		{
			Start = index;
			for (int i = 0; i < Count; i++)
			{
				if (i > 0)
					index++; // newline
				var element = this[i];
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
			for (int i= 0; i < Count; i++)
			{
				var element = this[i];

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
		
		internal T? Next(T element)
		{
			var index = IndexOf(element);
			return index < Count - 1 ? this[index + 1] : null;
		}

		internal T? Prev(T element)
		{
			var index = IndexOf(element);
			return index > 0 ? this[index - 1] : null;
		}

		// public IEnumerable<IDocumentElement> Enumerate(int start, int end)
		// {
		// 	for (int i = 0; i < Count; i++)
		// 	{
		// 		var element = this[i];
		// 		if (element.Start >= end)
		// 			break;
		// 		if (element.End <= start)
		// 			continue;
		// 		foreach (var child in element.Enumerate(start, end))
		// 		{
		// 			yield return child;
		// 		}
		// 	}
		// }

		public IEnumerable<IInlineElement> EnumerateInlines(int start, int end)
		{
			for (int i = 0; i < Count; i++)
			{
				var element = this[i];
				if (element.Start >= end)
					break;
				if (element.End <= start)
					continue;
				foreach (var inline in element.EnumerateInlines(start, end))
				{
					yield return inline;
				}
			}
		}
		
		public string GetText(int start, int length)
		{
			if (Separator != null) string.Join(Separator, EnumerateInlines(start, start + length));
			return string.Concat(EnumerateInlines(start, length));
		}
		
	}
}
