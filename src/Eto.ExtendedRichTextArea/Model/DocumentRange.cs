using Eto.Drawing;

namespace Eto.ExtendedRichTextArea.Model;

public class DocumentRange
{
	internal Document Document { get; private set; }

	public int Start { get; }
	public int Length => End - Start;
	public int End { get; private set; }
	public string Text
	{
		get => Document.GetText(Start, End) ?? string.Empty;
		set
		{
			Document.BeginEdit();
			Document.RemoveAt(Start, Length);
			End = Start + value.Length;
			_bounds = null;
			Document.InsertText(Start, value);
			Document.EndEdit();
		}
	}

	public int OriginalStart { get; }

	public bool Contains(int index) => index >= Start && index < End;

	List<RectangleF>? _bounds;

	public IEnumerable<RectangleF> Bounds
	{
		get
		{
			if (_bounds == null)
				CalculateBounds();
			return _bounds ?? Enumerable.Empty<RectangleF>();
		}
	}

	internal DocumentRange(Document document, int start, int end, int? originalStart = null)
	{
		Document = document;
		OriginalStart = originalStart ?? start;
		Start = Math.Min(start, end);
		End = Math.Max(start, end);
	}

	void CalculateBounds()
	{
		_bounds ??= new List<RectangleF>();
		_bounds.Clear();
		Chunk? lastChunk = null;
		foreach (var line in Document.EnumerateLines(Start))
		{
			if (line.Start >= End)
				break;

			RectangleF bounds = RectangleF.Empty;
			if (line.Count == 0)
			{
				bounds = line.Bounds;
				if (bounds.Width <= 0)
					bounds.Width = 8;
				_bounds.Add(bounds);
				continue;
			}

			foreach (var chunk in line)
			{
				var documentIndex = line.Start + chunk.Start;
				if (documentIndex >= End)
					break;
				if (documentIndex + chunk.Length <= Start)
					continue;

				if (bounds.IsEmpty)
				{
					bounds = chunk.Bounds;
					bounds.Y = line.Bounds.Y;
					bounds.Height = line.Bounds.Height;
					if (documentIndex < Start)
					{
						var point = chunk.GetPointAt(Start - documentIndex);
						if (point != null)
						{
							bounds.Width -= point.Value.X - bounds.X;
							bounds.X = point.Value.X;
						}
					}
				}
				else if (chunk.Bounds.Y != bounds.Y || chunk.Bounds.X < bounds.X)
				{
					_bounds.Add(bounds);
					bounds = chunk.Bounds;
					bounds.Y = line.Bounds.Y;
					bounds.Height = line.Bounds.Height;
				}
				else
				{
					// combine bounds
					bounds.Right = chunk.Bounds.Right;
					// bounds.Height = Math.Max(bounds.Height, chunk.Bounds.Height);
				}
				lastChunk = chunk;
			}
			if (!bounds.IsEmpty)
			{
				if (lastChunk != null)
				{
					var documentIndex = lastChunk.Element.DocumentStart;
					if (documentIndex + lastChunk.Length > End)
					{
						var point = lastChunk.GetPointAt(End - documentIndex);
						bounds.Width = point?.X - bounds.X ?? bounds.Width;
					}
				}
				_bounds.Add(bounds);
			}
		}
	}

	internal void Paint(Graphics graphics)
	{
		if (_bounds == null)
			CalculateBounds();
		if (_bounds == null)
			return;
		foreach (var bounds in _bounds)
		{
			graphics.FillRectangle(SystemColors.Highlight, bounds);
		}
	}

	public event EventHandler<EventArgs>? AttributesChanged;

	Attributes? _attributes;

	public Attributes Attributes
	{
		get => _attributes ??= Document.GetAttributes(Start, End);
		set
		{
			_bounds = null;
			_attributes = null;
			Document.SetAttributes(Start, End, value);
			AttributesChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	internal DocumentRange? Clone()
	{
		return new DocumentRange(Document, Start, End, OriginalStart);
	}

	internal void ReplaceWithBlocks(IEnumerable<IBlockElement> blocks)
	{
		if (blocks == null)
			throw new ArgumentNullException(nameof(blocks));

		var inserted = blocks.ToList();
		var isWholeDocumentReplace = Start == 0 && Length == Document.Length;

		if (isWholeDocumentReplace)
		{
			Document.BeginEdit();
			try
			{
				Document.Clear();
				foreach (var block in inserted)
					Document.Add(block);

				End = Document.Length;
				_bounds = null;
			}
			finally
			{
				Document.EndEdit();
			}
			return;
		}

		var originalDocumentLength = Document.Length;
		Document.BeginEdit();
		try
		{
			Document.RemoveAt(Start, Length);

			var insertAt = Start;
			var startIndex = 0;

			// If the first block is a paragraph, merge its inlines into the current paragraph.
			if (inserted.Count > 0 && inserted[0] is ParagraphElement firstParagraph)
			{
				foreach (var inline in firstParagraph.ToList())
				{
					if (inline.Clone() is not IInlineElement inlineClone)
						continue;

					Document.InsertAt(insertAt, inlineClone);
					insertAt += inlineClone.Length;
				}
				startIndex = 1;
			}

			for (var i = startIndex; i < inserted.Count; i++)
			{
				var block = inserted[i];
				Document.InsertAt(insertAt, block);
				insertAt += block.Length;
				if (i < inserted.Count - 1)
					insertAt += ((IBlockElement)Document).SeparatorLength;
			}

			var insertedLength = Document.Length - (originalDocumentLength - Length);
			End = Start + Math.Max(0, insertedLength);
			_bounds = null;
		}
		finally
		{
			Document.EndEdit();
		}
	}

	public static bool operator ==(DocumentRange? left, DocumentRange? right)
	{
		if (left is null && right is null)
			return true;
		if (left is null || right is null)
			return false;
		return left.Document == right.Document
			&& left.Start == right.Start
			&& left.End == right.End
			&& left.OriginalStart == right.OriginalStart;
	}

	public static bool operator !=(DocumentRange? left, DocumentRange? right) => !(left == right);

	public override bool Equals(object? obj)
	{
		if (obj is DocumentRange other)
			return this == other;
		return false;
	}

	public IEnumerable<IElement> GetElements(bool trim) => Document.Enumerate(Start, End, trim, false);

	public IEnumerable<IElement> GetElements() => Document.Enumerate(Start, End, true, false);

	public override int GetHashCode()
	{
#if NET
		return HashCode.Combine(Start, End, OriginalStart);
#else
		unchecked
		{
			int hash = 17;
			hash = hash * 23 + Start.GetHashCode();
			hash = hash * 23 + End.GetHashCode();
			hash = hash * 23 + OriginalStart.GetHashCode();
			return hash;
		}
#endif
	}

	public void ReplaceWithList(ListType listType)
	{
		var doc = Document;
		doc.BeginEdit();
		doc.EnsureValid();
		int? start = null;
		int docStart = Start;
		var selectionElements = GetElements(false).OfType<IBlockElement>().ToList();
		doc.EnsureValid();

		// Toggle off: if every selected block is already a ListElement of the requested type,
		// convert all covered items back to plain paragraphs instead of re-wrapping them.
		if (selectionElements.Count > 0 && selectionElements.All(e => e is ListElement le && le.Type == listType))
		{
			var lastElement = selectionElements.Last();
			var lastElementEnd = lastElement.DocumentStart + lastElement.Length;
			var lastElementLength = lastElement.Length - (lastElementEnd - End);
			for (int i = 0; i < selectionElements.Count; i++)
			{
				var existingList = (ListElement)selectionElements[i];
				var elementStart = existingList.DocumentStart;
				start ??= elementStart;

				var listStart = i == 0 ? Start - elementStart : 0;
				var listEnd = i == selectionElements.Count - 1 ? lastElementLength : existingList.Length;
				var listItems = existingList.Enumerate(listStart, listEnd, false, false).OfType<ListItemElement>().ToList();
				doc.EnsureValid();

				for (int i1 = 0; i1 < listItems.Count; i1++)
				{
					ListItemElement item = listItems[i1];
					if (i1 == 0 && existingList.IndexOf(item) > 0)
					{
						// split so items before the selection stay in the original list
						var docIdx = doc.IndexOf(existingList);
						var right = ((IBlockElement)existingList).Split(item.Start);
						if (right is IBlockElement rightBlock)
						{
							doc.Insert(docIdx + 1, rightBlock);
							existingList = (ListElement)rightBlock;
							start = existingList.DocumentStart;
						}
					}

					var docInsertIdx = doc.IndexOf(existingList);
					existingList.Remove(item);

					var para = new ParagraphElement();
					para.Attributes = item.Attributes?.Clone();
					para.AddRange(item);

					// Convert the list item indent level back to leading tabs
					if (item.Level > 0)
						para.Insert(0, new TextElement { Text = new string('\t', item.Level) });

					doc.Insert(docInsertIdx, para);

					// keep pointing at the (now smaller) list that trails the inserted paragraph
					if (existingList.Count == 0)
					{
						doc.Remove(existingList);
						existingList = null!;
						break;
					}
				}
			}

			doc.EndEdit();
			return;
		}

		var list = new ListElement { Type = listType };
		if (selectionElements.Count > 0)
		{
			var lastElement = selectionElements.Last();
			var lastElementEnd = lastElement.DocumentStart + lastElement.Length;
			var lastElementLength = lastElement.Length - (lastElementEnd - End);
			for (int i = 0; i < selectionElements.Count; i++)
			{
				IBlockElement element = selectionElements[i];

				var elementStart = element.DocumentStart; // only valid when i == 0
				start ??= elementStart;

				if (element is ParagraphElement para)
				{
					doc.Remove(element);

					// Convert leading tabs to the list item indent level
					int level = 0;
					while (para.Count > 0 && para[0] is TextElement firstText)
					{
						int t = 0;
						while (t < firstText.Text.Length && firstText.Text[t] == '\t')
							t++;
						if (t == 0)
							break;
						level += t;
						var remaining = firstText.Text.Substring(t);
						if (remaining.Length == 0)
							para.Remove(firstText);
						else
						{
							firstText.Text = remaining;
							break;
						}
					}

					var listItem = new ListItemElement(para);
					listItem.Level = level;
					list.Add(listItem);
				}
				else if (element is ListElement existingList)
				{
					var listStart = i == 0 ? Start - elementStart : 0;
					var listEnd = i == selectionElements.Count - 1 ? lastElementLength : existingList.Length;
					var listElements = existingList.Enumerate(listStart, listEnd, false, false).OfType<ListItemElement>().ToList();
					doc.EnsureValid();

					for (int i1 = 0; i1 < listElements.Count; i1++)
					{
						ListItemElement item = listElements[i1];
						if (i1 == 0 && existingList.IndexOf(item) > 0)
						{
							// split existing list to keep the items before the selection in the original list
							var docIdx = doc.IndexOf(existingList);
							var right = ((IBlockElement)existingList).Split(item.Start);
							if (right is IBlockElement rightBlock)
							{
								doc.Insert(docIdx + 1, rightBlock);
								existingList = (ListElement)rightBlock;
								start = existingList.DocumentStart;
							}
						}
						existingList.Remove(item);
						list.Add(item);
					}
					if (existingList.Count == 0)
						doc.Remove(existingList);
				}
				else
				{
					doc.Remove(element);
				}
			}
		}
		if (list.Count == 0)
			list.Add(new ListItemElement());
		doc.InsertAt(start ?? Start, list);

		// Merge with adjacent lists of the same type.
		var listIdx = doc.IndexOf(list);

		// Right neighbour: drain its items into 'list', then remove it from the document.
		if (listIdx + 1 < doc.Count && doc[listIdx + 1] is ListElement rightNeighbour && list.Type == rightNeighbour.Type)
		{
			var itemsToMove = rightNeighbour.ToList();
			foreach (var item in itemsToMove)
				rightNeighbour.Remove(item);
			doc.Remove(rightNeighbour);
			foreach (var item in itemsToMove)
				list.Add(item);
		}

		// Left neighbour: drain 'list' items into it, then remove 'list' from the document.
		if (listIdx > 0 && doc[listIdx - 1] is ListElement leftNeighbour && list.Type == leftNeighbour.Type)
		{
			var itemsToMove = list.ToList();
			foreach (var item in itemsToMove)
				list.Remove(item);
			doc.Remove(list);
			foreach (var item in itemsToMove)
				leftNeighbour.Add(item);
		}

		doc.EndEdit();
	}
}
