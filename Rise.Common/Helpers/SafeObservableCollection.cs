using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace Rise.Common.Helpers
{
    /// <summary>
    /// An observable collection class with thread safety and ranges in mind.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    public partial class SafeObservableCollection<T> : ObservableCollection<T>
    {
        private readonly object _syncLock = new();

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="SafeObservableCollection{T}"/> class.
        /// </summary>
        public SafeObservableCollection()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="SafeObservableCollection{T}"/> class.
        /// </summary>
        /// <param name="collection">The collection from which the
        /// elements are copied.</param>
        /// <exception cref="ArgumentNullException">The collection
        /// parameter cannot be null.</exception>
        public SafeObservableCollection(IEnumerable<T> collection)
            : base(collection)
        {
        }

        /// <summary>
        /// Adds the elements from the specified collection
        /// to the end of this collection.
        /// </summary>
        public void AddRange(IEnumerable<T> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            IList addedItems = items as IList ?? items.ToList();
            if (addedItems.Count == 0)
                return;

            CheckReentrancy();

            int startIndex = Count;
            foreach (T item in addedItems)
            {
                Items.Add(item);
            }

            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add, addedItems, startIndex));
        }

        /// <summary>
        /// Removes the elements in the specified
        /// collection from this collection.
        /// </summary>
        public void RemoveRange(IEnumerable<T> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            CheckReentrancy();

            bool raiseEvents = false;
            foreach (T item in items)
            {
                if (Items.Remove(item))
                    raiseEvents = true;
            }

            if (raiseEvents)
            {
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
                OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));

                OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Reset));
            }
        }
    }

    public partial class SafeObservableCollection<T>
    {
        private readonly Dictionary<NotifyCollectionChangedEventHandler, SynchronizationContext> CollectionChangedEvents = new();
        private readonly Dictionary<PropertyChangedEventHandler, SynchronizationContext> PropertyChangedEvents = new();

        public override event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add
            {
                if (value == null) return;
                lock (_syncLock)
                    CollectionChangedEvents[value] = SynchronizationContext.Current;
            }
            remove
            {
                if (value == null) return;
                lock (_syncLock)
                    _ = CollectionChangedEvents.Remove(value);
            }
        }

        protected override event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                if (value == null) return;
                lock (_syncLock)
                    PropertyChangedEvents[value] = SynchronizationContext.Current;
            }
            remove
            {
                if (value == null) return;
                lock (_syncLock)
                    _ = PropertyChangedEvents.Remove(value);
            }
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            KeyValuePair<NotifyCollectionChangedEventHandler, SynchronizationContext>[] snapshot;
            lock (_syncLock)
            {
                if (CollectionChangedEvents.Count == 0)
                    return;

                snapshot = CollectionChangedEvents.ToArray();
            }

            using (BlockReentrancy())
            {
                foreach (var entry in snapshot)
                {
                    var handler = entry.Key;
                    var context = entry.Value;
                    if (context == null || context == SynchronizationContext.Current)
                        handler.Invoke(this, e);
                    else
                        context.Post(s => handler.Invoke(s, e), this);
                }
            }
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            KeyValuePair<PropertyChangedEventHandler, SynchronizationContext>[] snapshot;
            lock (_syncLock)
            {
                if (PropertyChangedEvents.Count == 0)
                    return;

                snapshot = PropertyChangedEvents.ToArray();
            }

            foreach (var entry in snapshot)
            {
                var handler = entry.Key;
                var context = entry.Value;
                if (context == null || context == SynchronizationContext.Current)
                    handler.Invoke(this, e);
                else
                    context.Post(s => handler.Invoke(s, e), this);
            }
        }
    }
}
