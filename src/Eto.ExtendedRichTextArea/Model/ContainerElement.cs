using Eto.Drawing;

using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Eto.ExtendedRichTextArea.Model
{
	public interface IContainerElement : IElement
	{
		RectangleF Bounds { get; }
		SizeF Measure(Attributes defaultAttributes, SizeF availableSize, PointF location);
		int GetIndexAt(PointF point);
		PointF? GetPointAt(int start);
		IEnumerable<Chunk> EnumerateChunks(int start, int end);
		IEnumerable<Line> EnumerateLines(int start, bool forward = true);
	}
	
	public static class ElementExtensions
	{
		public static Document? GetDocument(this IElement element)
		{
			var parent = element.Parent;
			while (parent?.Parent != null)
			{
				parent = parent.Parent;
			}
			return parent as Document;
		}
		
	}
	
	public abstract class ContainerElement<T> : Collection<T>, IContainerElement
		where T : class, IElement
	{
		public int Start { get; internal set; }
		public int Length { get; protected set; }
		public int End => Start + Length;
		public RectangleF Bounds { get; private set; }
		public int DocumentStart => Start + Parent?.DocumentStart ?? 0;
		
		
		public IElement? Parent { get; private set; }
		
		IElement? IElement.Parent
		{
			get => Parent;
			set => Parent = value;
		}

		protected virtual string? Separator => null;

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
							Add(element);
						}
					}
					else
					{
						var element = CreateElement();
						element.Text = value;
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
			item.Parent = this;
			Adjust(index, item.Length);
			MeasureIfNeeded();
		}

		protected override void RemoveItem(int index)
		{
			var element = this[index];
			element.Parent = null;
			base.RemoveItem(index);
			Adjust(index, -element.Length);
			MeasureIfNeeded();
		}

		protected override void SetItem(int index, T item)
		{
			var old = this[index];
			old.Parent = null;
			base.SetItem(index, item);
			item.Parent = this;
			Adjust(index, item.Length - old.Length);
			MeasureIfNeeded();
		}

		internal abstract ContainerElement<T> Create();
		internal abstract T CreateElement();

		public SizeF Measure(Attributes defaultAttributes, SizeF availableSize, PointF location)
		{
			var size = MeasureOverride(defaultAttributes, availableSize, location);
			Bounds = new RectangleF(location, size);
			return size;
		}

		protected abstract SizeF MeasureOverride(Attributes defaultAttributes, SizeF availableSize, PointF location);
		
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

		public IEnumerable<(string text, int start)> EnumerateWords(int start, bool forward)
		{
			if (forward)
			{
				for (int i = 0; i < Count; i++)
				{
					var element = this[i];
					foreach (var word in element.EnumerateWords(start, forward))
					{
						yield return (word.text, word.start + element.Start);
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
						yield return (word.text, word.start + element.Start);
					}
				}
			}
		}

		public void InsertAt(int start, T element)
		{
			element.Start = start;
			for (int i = 0; i < Count; i++)
			{
				var child = this[i];
				if (start > child.End)
					continue;

				if (start > child.Start)
				{
					// split if needed
					var right = child.Split(start - child.Start);
					if (right != null && right is T newChild)
					{
						Insert(i, newChild);
					}
					i++; // insert after
				}
					
				Insert(i, element);
				Adjust(i, element.Length);
				MeasureIfNeeded();
				return;
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
			if (length < 0)
				throw new ArgumentOutOfRangeException(nameof(length), "Length must be greater than or equal to zero");
			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index), "Index must be greater than or equal to zero");
			var originalLength = length;
			var separatorLength = Separator?.Length ?? 0;
			// go backwards so we don't have to update indexes as we remove
			for (int i = Count - 1; i >= 0; i--)
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
					length -= element.Length + separatorLength;
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
				else if (end > element.End && element is ParagraphElement paragraph)
				{
					// merge the next paragraph into this one
					if (i + 1 < Count)
					{
						var nextElement = this[i + 1];
						if (nextElement is ParagraphElement nextParagraph)
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
							length -= separatorLength;
							i++; // process this paragraph again, now that it's been merged
						}
					}
				}
				else
				{
					var removeStart = start - element.Start;
					var removeLength = length;
					if (removeStart < 0)
					{
						removeLength += removeStart;
						removeStart = 0;
					}
						
					if (removeLength > 0)
						length -= element.RemoveAt(removeStart, removeLength);
				}
				if (element.Length == 0)
				{
					Remove(element);
					length -= separatorLength;
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
			ContainerElement<T> CreateNew()
			{
				var newElement = Create();
				return newElement;
			}

			ContainerElement<T>? newRun = null;
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
			var childIndex = 0;
			for (int i = 0; i < Count; i++)
			{
				if (i > 0)
					childIndex++; // newline
				var element = this[i];
				element.Recalculate(childIndex);
				childIndex += element.Length;
			}
			Length = childIndex;
		}

		public abstract PointF? GetPointAt(int index);
		
        public virtual int GetIndexAt(PointF point)
        {
			if (point.Y < Bounds.Top)
				return 0;
			if (point.Y > Bounds.Bottom)
				return Length;
			for (int i = 0; i < Count; i++)
			{
				var element = this[i];

				if (element is IContainerElement container)
				{
					// too far, break!
					if (point.Y < container.Bounds.Top)
						break;
					if (point.Y >= container.Bounds.Bottom)
						continue;
						
					// traverse containers
					var index = container.GetIndexAt(point);
					if (index >= 0)
						return index + element.Start;
				}
			}
			return Length;
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

		public virtual IEnumerable<Chunk> EnumerateChunks(int start, int end)
		{
			for (int i = 0; i < Count; i++)
			{
				var element = this[i];
				if (element.Start >= end)
					break;
				if (element.End <= start)
					continue;
				if (element is IContainerElement container)
				{
					var containerStart = Math.Max(start - element.Start, 0);
					var containerEnd = Math.Min(end - element.Start, element.Length);
					foreach (var inline in container.EnumerateChunks(containerStart, containerEnd))
					{
						yield return inline;
					}
				}
			}
		}
		public virtual IEnumerable<Line> EnumerateLines(int start, bool forward = true)
		{
			var collection = forward ? this : this.Reverse();
			foreach (var element in collection)
			{
				if (forward && element.End < start)
					continue;
				else if (!forward && element.Start > start)
					continue;
				if (element is IContainerElement container)
				{
					var containerStart = Math.Max(start - element.Start, 0);
					foreach (var line in container.EnumerateLines(containerStart, forward))
					{
						yield return line;
					}
				}
			}
			// empty paragraph
			if (Count == 0)
				yield return new Line { Start = 0, DocumentStart = DocumentStart, Bounds = Bounds };
			
		}
		
		public string GetText(int start, int length)
		{
			if (Separator != null) string.Join(Separator, EnumerateInlines(start, start + length));
			return string.Concat(EnumerateInlines(start, length));
		}

		public override string ToString() => Text;

	}
}
