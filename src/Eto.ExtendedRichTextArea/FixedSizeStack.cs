namespace Eto.ExtendedRichTextArea
{
	class FixedSizeStack<T>
	{
		private readonly int _maxCapacity;
		private readonly List<T> _buffer;

		public int Count => _buffer.Count;

		public FixedSizeStack(int maxCapacity)
		{
			_buffer = new List<T>();
			_maxCapacity = maxCapacity;
		}

		public void Push(T item)
		{
			if (_buffer.Count >= _maxCapacity)
				_buffer.RemoveAt(0);
			_buffer.Add(item);
		}

		public T Pop()
		{
			if (_buffer.Count == 0)
				throw new InvalidOperationException("Stack is empty.");
			int lastIndex = _buffer.Count - 1;
			var item = _buffer[lastIndex];
			_buffer.RemoveAt(lastIndex);
			return item;
		}

		public T Peek()
		{
			if (_buffer.Count == 0)
				throw new InvalidOperationException("Stack is empty.");
			return _buffer[_buffer.Count - 1];
		}

		internal void Clear()
		{
			_buffer.Clear();
		}
	}
}
