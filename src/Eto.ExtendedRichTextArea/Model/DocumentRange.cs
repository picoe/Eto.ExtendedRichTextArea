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

	public void SetAttributes(Attributes? selectionAttributes)
	{
		_bounds = null;
		Document.SetAttributes(Start, End, selectionAttributes);
	}

	public Attributes GetAttributes() => Document.GetAttributes(Start, End);

	internal DocumentRange? Clone()
	{
		return new DocumentRange(Document, Start, End, OriginalStart);
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
}
