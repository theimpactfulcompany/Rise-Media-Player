using System;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Rise.Common.Helpers
{
    /// <summary>
    /// Listens for changes to a dependency property.
    /// </summary>
    /// <typeparam name="T">Type of value returned by the property.</typeparam>
    public sealed class DependencyPropertyWatcher<T> : IDisposable
    {
        /// <summary>
        /// The object being watched for changes.
        /// </summary>
        public DependencyObject WatchedObject { get; }

        /// <summary>
        /// The property that's currently being watched.
        /// </summary>
        public DependencyProperty WatchedProperty { get; }

        private readonly long _token;
        private bool _disposed;

        /// <summary>
        /// Represents the method that will handle events that occur when a
        /// <see cref="DependencyPropertyWatcher{T}"/> fires a change notification.
        /// </summary>
        /// <param name="sender">The watcher that produced the notification.</param>
        /// <param name="newValue">The new value for the property being watched.</param>
        public delegate void PropertyWatcherFiredEventHandler(DependencyPropertyWatcher<T> sender, T newValue);

        /// <summary>
        /// Occurs when the watched property's value changes.
        /// </summary>
        public event PropertyWatcherFiredEventHandler PropertyChanged;

        /// <summary>
        /// Creates a new watcher for the provided property's value
        /// in the provided object.
        /// </summary>
        public DependencyPropertyWatcher(DependencyObject obj, DependencyProperty property)
        {
            WatchedObject = obj ?? throw new ArgumentNullException(nameof(obj));
            WatchedProperty = property ?? throw new ArgumentNullException(nameof(property));

            _token = obj.RegisterPropertyChangedCallback(property, PropertyChangedCallback);
        }

        private void PropertyChangedCallback(DependencyObject sender, DependencyProperty dp)
        {
            if (_disposed)
                return;

            _ = sender.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (_disposed)
                    return;

                object val = sender.GetValue(dp);
                if (val is T typedValue)
                    PropertyChanged?.Invoke(this, typedValue);
                else if (val == null && default(T) == null)
                    PropertyChanged?.Invoke(this, default);
            });
        }

        /// <summary>
        /// Unhooks the property change callback from the watched object.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            WatchedObject.UnregisterPropertyChangedCallback(WatchedProperty, _token);
        }
    }
}
