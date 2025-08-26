using Eto.Drawing;

using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Eto.ExtendedRichTextArea.Model
{

	public abstract class ContainerElement<T> : Collection<T>, IBlockElement
		where T : class, IElement
	{
		public int Start { get; internal set; }
		public int Length { get; private set; }
		public int End => Start + Length;
		public RectangleF Bounds { get; private set; }
		public int DocumentStart => Start + Parent?.DocumentStart ?? 0;


		public IBlockElement? Parent { get; private set; }

		IBlockElement? IElement.Parent
		{
			get => Parent;
			set => Parent = value;
		}

		public virtual Attributes? Attributes { get; set; }

		protected virtual string? Separator => null;
		protected virtual int SeparatorLength => Separator?.Length ?? 0;

		string? IBlockElement.Separator => Separator;
		int IBlockElement.SeparatorLength => SeparatorLength;

		int IElement.Start
		{
			get => Start;
			set => Start = value;
		}

		public string Text
		{
			get => GetText();
			set
			{
				BeginEdit();
				SetText(value);
				EndEdit();
			}
		}

		protected virtual void SetText(string value)
		{
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
		}

		protected virtual string GetText()
		{
			if (Separator != null)
				return string.Join(Separator, this.Select(r => r.Text));
				
			return string.Concat(this.Select(r => r.Text));
		}

		protected override void ClearItems()
		{
			base.ClearItems();
			Length = 0;
			MeasureIfNeeded();
		}

		protected override void InsertItem(int index, T item)
		{
			var rightItem = index < Count ? this[index] : null;
			base.InsertItem(index, item);
			item.Parent = this;
			var adjust = item.Length;
			if (rightItem != null)
			{
				item.Start = rightItem.Start;
				adjust += SeparatorLength;
			}
			else
			{
				item.Start = index > 0 ? Length + SeparatorLength : 0;
				if (Count > 1)
					adjust += SeparatorLength;
			}
			Adjust(index, adjust);
			MeasureIfNeeded();
		}

		protected override void RemoveItem(int index)
		{
			var element = this[index];
			element.Parent = null;
			element.Start = 0;
			base.RemoveItem(index);
			var adjust = element.Length;
			if (Count > 0)
				adjust += SeparatorLength;
			Adjust(index - 1, -adjust);
			MeasureIfNeeded();
		}

		protected override void SetItem(int index, T item)
		{
			var old = this[index];
			old.Parent = null;
			base.SetItem(index, item);
			item.Parent = this;
			item.Start = old.Start;
			Adjust(index, item.Length - old.Length);
			MeasureIfNeeded();
		}

		protected abstract ContainerElement<T> Create();
		protected abstract T CreateElement();

		IElement IBlockElement.CreateElement() => CreateElement();

		public SizeF Measure(Attributes defaultAttributes, SizeF availableSize, PointF location)
		{
			var childAttributes = defaultAttributes.Merge(Attributes, false);
			var size = MeasureOverride(childAttributes, availableSize, location);
			Bounds = new RectangleF(location, size);
			return size;
		}

		protected abstract SizeF MeasureOverride(Attributes defaultAttributes, SizeF availableSize, PointF location);

		internal (T? child, int index, int position) FindAt(int position)
		{
			for (int i = 0; i < Count; i++)
			{
				var element = this[i];
				if (position <= element.Start + element.Length)
					return (element, i, Math.Max(0, position - element.Start));
			}
			return (null, -1, position);
		}

		internal int FindIndexAt(int position)
		{
			for (int i = 0; i < Count; i++)
			{
				var element = this[i];
				if (position <= element.Start + element.Length)
					return i;
			}
			return -1;
		}

		public IEnumerable<(string text, int start)> EnumerateWords(int start, bool forward)
		{
			var separatorLength = SeparatorLength;
			if (forward)
			{
				for (int i = 0; i < Count; i++)
				{
					var element = this[i];
					if (start >= element.End)
						continue;
					if (start < element.Start && separatorLength > 0)
					{
						yield return (string.Empty, start);
						start += separatorLength;
					}
					foreach (var word in element.EnumerateWords(start - element.Start, forward))
					{
						yield return (word.text, word.start + element.Start);
					}
					start = element.End;
				}
			}
			else
			{
				for (int i = Count - 1; i >= 0; i--)
				{
					var element = this[i];
					if (start <= element.Start)
						continue;
					if (start >= element.End && separatorLength > 0)
					{
						start -= separatorLength;
						yield return (string.Empty, start);
					}
					foreach (var word in element.EnumerateWords(start - element.Start, forward))
					{
						yield return (word.text, word.start + element.Start);
					}
					start = element.Start;
				}
			}
		}

		public virtual bool InsertAt(int start, IElement element)
		{
			var (child, index, position) = FindAt(start);
			var childElement = child;

			if (childElement?.InsertAt(position, element) == true)
			{
				return true;
			}

			if (element is not T newElement)
				return false;

			if (index < 0)
			{
				Add(newElement);
				return true;
			}

			// split existing element if possible
			var existing = this[index];
			if (existing.Split(start - existing.Start) is T rightElement)
			{
				Insert(index + 1, rightElement);
			}
			// if we can't split, just insert after the existing element (if not empty)
			Insert(index + 1, newElement);
			return true;
		}


		public void Adjust(int startIndex, int length)
		{
			AdjustLength(length);
			for (int j = startIndex + 1; j < Count; j++)
			{
				this[j].Start += length;
			}
		}

		public int RemoveAt(int start, int length)
		{
			if (length < 0)
				throw new ArgumentOutOfRangeException(nameof(length), "Length must be greater than or equal to zero");
			if (start < 0)
				throw new ArgumentOutOfRangeException(nameof(start), "Index must be greater than or equal to zero");
			BeginEdit();
			var originalLength = length;
			var separatorLength = SeparatorLength;
			// go backwards so we don't have to update indexes as we remove
			for (int i = Count - 1; i >= 0; i--)
			{
				// if we've removed all the characters, we're done
				if (length <= 0)
					break;

				var element = this[i];
				var elementStart = start;
				var elementEnd = elementStart + length;
				if (elementEnd < element.Start || elementStart > element.End)
					continue;
				if (elementStart <= element.Start && elementEnd >= element.End && length >= element.Length + separatorLength)
				{
					Remove(element);
					length -= Count == 0 ? element.Length : element.Length + separatorLength;
					continue;
				}

				if (elementStart >= element.Start && elementEnd < element.End)
				{
					var removed = element.RemoveAt(elementStart - element.Start, length);
					length -= removed;
				}
				else if (elementEnd > element.End && element is ParagraphElement paragraph)
				{
					// merge the next paragraph into this one
					if (i + 1 < Count)
					{
						var nextElement = this[i + 1];
						if (nextElement is ParagraphElement nextParagraph)
						{
							// add the elements to this paragraph (if any)
							Remove(nextElement);
							foreach (var child in nextParagraph)
							{
								paragraph.Add(child);
							}
							if (Count > 0)
								length -= separatorLength;
							i++; // process this paragraph again, now that it's been merged
						}
					}
				}
				else
				{
					var removeStart = elementStart - element.Start;
					var removeLength = length;
					if (removeStart < 0)
					{
						removeLength += removeStart;
						removeStart = 0;
					}

					if (removeLength > 0)
					{
						var removed = element.RemoveAt(removeStart, removeLength);
						length -= removed;
					}
				}
				if (element.Length == 0 && length > separatorLength)
				{
					Remove(element);
					if (Count > 0)
						length -= separatorLength;
				}
			}
			EndEdit();
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

		IElement? IElement.Split(int start) => Split(start);

		protected IElement? Split(int start)
		{
			if (start >= Length)
				return null;
				
			ContainerElement<T> CreateNew()
			{
				var newElement = Create();
				newElement.Attributes = Attributes?.Clone();
				return newElement;
			}

			ContainerElement<T>? newRun = null;

			if (Start == start)
			{
				newRun = CreateNew();
				for (int i = 0; i < Count; i++)
				{
					var element = this[i];
					newRun.Add(element);
				}
				Clear();
				Length = 0;
				Parent?.Adjust(Parent.IndexOf(this), -newRun.Length);
				return newRun;
			}

			for (int i = 0; i < Count; i++)
			{
				var element = this[i];
				if (element.Start >= start)
				{
					Remove(element);
					i--;
					newRun ??= CreateNew();
					element.Start = newRun.Length;
					newRun.Add(element);
				}
				else if (element.End > start)
				{
					newRun ??= CreateNew();
					if (element.Split(start - element.Start) is T newElement)
					{
						newElement.Start = newRun.Length;
						newRun.Add(newElement);
					}
				}
			}
			var offset = Length - start;
			Parent?.Adjust(Parent.IndexOf(this), -offset);
			return newRun;
		}

		public abstract PointF? GetPointAt(int start, out Line? line);

		public abstract int GetIndexAt(PointF point);

		public IEnumerable<IElement> Enumerate(int start, int end, bool trimInlines)
		{
			for (int i = 0; i < Count; i++)
			{
				var element = this[i];
				if (element.Start > end)
					break;
				if (element.End < start)
					continue;
				yield return element;
				var elementStart = Math.Max(start - element.Start, 0);
				var elementEnd = Math.Min(end - element.Start, element.Length);
				foreach (var child in element.Enumerate(elementStart, elementEnd, trimInlines))
				{
					yield return child;
				}
			}
		}

		public IEnumerable<IInlineElement> EnumerateInlines(int start, int end, bool trim)
		{
			for (int i = 0; i < Count; i++)
			{
				var element = this[i];
				if (element.Start > end)
					break;
				if (element.End < start)
					continue;
				var elementStart = Math.Max(start - element.Start, 0);
				var elementEnd = Math.Min(end - element.Start, element.Length);
				foreach (var inline in element.EnumerateInlines(elementStart, elementEnd, trim))
				{
					yield return inline;
				}
			}
		}

		public abstract IEnumerable<Chunk> EnumerateChunks(int start, int end);
		public abstract IEnumerable<Line> EnumerateLines(int start, bool forward = true);

		public string GetText(int start, int length)
		{
			var inlineText = EnumerateInlines(start, start + length, true).Select(r => r.Text);
			return Separator != null ? string.Join(Separator, inlineText) : string.Concat(inlineText);
		}

		public override string ToString() => Text;

		public abstract void Paint(Graphics graphics, RectangleF clipBounds);


		public virtual Attributes GetAttributes(Attributes defaultAttributes, int start, int end)
		{
			// TODO: move this to ContainerElement?
			bool isRange = end > start;
			Attributes? attributes = null;
			foreach (var block in this)
			{
				if (end < block.Start)// || (end == paragraph.Start && isRange))
					break;
				if (start > block.End || (start == block.End && isRange))
					continue;

				var paragraphAttributes = defaultAttributes.Merge(block.Attributes, false);

				if (block is IList blockList)
				{

					var paragraphStart = start - block.Start;
					var paragraphEnd = end - block.Start;
					foreach (IElement inline in blockList)
					{
						if (paragraphEnd <= inline.Start)// || (paragraphEnd == inline.Start && isRange))
							break;
						if (paragraphStart > inline.End || (paragraphStart == inline.End && isRange))
							continue;

						if (attributes == null)
							attributes = paragraphAttributes.Merge(inline.Attributes, true);
						else
							attributes.ClearUnmatched(paragraphAttributes.Merge(inline.Attributes, false));
					}
				}

				if (attributes == null)
					attributes = paragraphAttributes.Clone();
			}
			return attributes ?? defaultAttributes.Clone();
		}

		internal static Attributes? UpdateAttributes(Attributes? attributes, Attributes? spanAttributes)
		{
			var existingAttributes = spanAttributes;

			Attributes? newAttributes;
			if (existingAttributes != null && attributes != null)
			{
				// need to merge the attributes into a new copy
				newAttributes = existingAttributes.Merge(attributes, true);
			}
			else if (existingAttributes == null)
			{
				// just use a copy of the new attributes
				newAttributes = attributes?.Clone();
			}
			else
			{
				newAttributes = null;
			}

			return newAttributes;
		}

		public virtual void SetAttributes(int start, int end, Attributes? attributes)
		{
			for (int i = 0; i < Count; i++)
			{
				var element = this[i];
				if (element == null)
					continue;
				if (end <= element.Start)
					break;
				if (start >= element.End)
					continue;

				if (element is IBlockElement block)
				{
					block.SetAttributes(Math.Max(0, start - element.Start), Math.Min(end - element.Start, element.Length), attributes);
					continue;
				}

				IElement applySpan = element;
				var elementStart = element.Start;
				if (start > elementStart && start < elementStart + element.Length)
				{
					// need to split and apply attributes to right side only
					if (element.Split(start - elementStart) is T right)
					{
						Insert(i + 1, right);

						applySpan = right; // apply new attributes to the right side
						elementStart = right.Start;
						if (end > elementStart && end < elementStart + applySpan.Length)
						{
							// need to split again as the end is in the middle of the right side
							if (applySpan.Split(end - elementStart) is T right2)
							{
								Insert(i + 2, right2);
							}
						}
					}
				}
				if (end > elementStart && end < elementStart + applySpan.Length)
				{
					// need to split and apply attributes to left side
					if (applySpan.Split(end - elementStart) is T right)
					{
						Insert(i + 1, right);
					}
				}
				applySpan.Attributes = UpdateAttributes(attributes, applySpan.Attributes);

			}
		}

		internal void AdjustLength(int length)
		{
			Length += length;
			if (Parent != null)
			{
				var idx = Parent.IndexOf(this);
				if (idx >= 0)
					Parent.Adjust(idx, length);
			}
		}

		void IBlockElement.AdjustLength(int length) => AdjustLength(length);

		public virtual bool IsValid()
		{
			var index = 0;
			if (Count == 0)
				return true;
			var separatorLength = SeparatorLength;
			for (int i = 0; i < Count; i++)
			{
				if (i > 0)
					index += separatorLength;

				var item = this[i];
				if (item.Start != index)
					return false;
				if (item is IBlockElement block && !block.IsValid())
					return false;
				index += item.Length;
			}
			return Length == index;
		}

		protected virtual ContainerElement<T> Clone()
		{
			var clone = Create();
			clone.Attributes = Attributes?.Clone();
			clone.Start = Start;
			foreach (var item in this)
			{
				var clonedItem = item.Clone() as T;
				if (clonedItem != null)
				{
					clonedItem.Parent = clone;
					clone.Add(clonedItem);
				}
			}
			return clone;
		}

		object ICloneable.Clone() => Clone();

		public void AddRange(IEnumerable<T> items)
		{
			BeginEdit();
			foreach (var item in items)
			{
				if (item == null)
					continue;
				Add(item);
			}
			EndEdit();
		}
	}
}
