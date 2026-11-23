using System.Collections;
using System.Collections.Generic;
using Windows.Foundation.Collections;

namespace Rise.Effects
{
    internal sealed class ObservableVector<T> : IObservableVector<T>
    {
        private readonly List<T> _base = new List<T>();

        public T this[int index]
        {
            get => _base[index];
            set
            {
                _base[index] = value;
                VectorChanged?.Invoke(this, new VectorChangedEventArgs(CollectionChange.ItemChanged, (uint)index));
            }
        }

        public int Count => _base.Count;
        public bool IsReadOnly => false;

        public event VectorChangedEventHandler<T> VectorChanged;

        public int IndexOf(T item) => _base.IndexOf(item);

        public void Add(T item)
        {
            _base.Add(item);
            VectorChanged?.Invoke(this, new VectorChangedEventArgs(CollectionChange.ItemInserted, (uint)(_base.Count - 1)));
        }

        public void Insert(int index, T item)
        {
            _base.Insert(index, item);
            VectorChanged?.Invoke(this, new VectorChangedEventArgs(CollectionChange.ItemInserted, (uint)index));
        }

        public bool Remove(T item)
        {
            int index = _base.IndexOf(item);
            if (index < 0)
                return false;

            _base.RemoveAt(index);
            VectorChanged?.Invoke(this, new VectorChangedEventArgs(CollectionChange.ItemRemoved, (uint)index));
            return true;
        }

        public void RemoveAt(int index)
        {
            _base.RemoveAt(index);
            VectorChanged?.Invoke(this, new VectorChangedEventArgs(CollectionChange.ItemRemoved, (uint)index));
        }

        public void Clear()
        {
            _base.Clear();
            VectorChanged?.Invoke(this, new VectorChangedEventArgs(CollectionChange.Reset, 0));
        }

        public bool Contains(T item) => _base.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => _base.CopyTo(array, arrayIndex);
        public IEnumerator<T> GetEnumerator() => _base.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _base.GetEnumerator();
    }

    internal sealed class VectorChangedEventArgs : IVectorChangedEventArgs
    {
        public CollectionChange CollectionChange { get; }
        public uint Index { get; }

        public VectorChangedEventArgs(CollectionChange change, uint index)
        {
            CollectionChange = change;
            Index = index;
        }
    }
}
