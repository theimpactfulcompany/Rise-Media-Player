using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Rise.Data.ViewModels
{
    /// <summary>
    /// Base ViewModel implementation, wraps a <typeparamref name="TModel"/>
    /// object for the Model-View-ViewModel pattern and contains methods
    /// to handle property changes.
    /// </summary>
    /// <typeparam name="TModel">Type of the underlying model.</typeparam>
    public abstract class ViewModel<TModel> : ViewModel, IEquatable<TModel>, IEquatable<ViewModel<TModel>>
    {
        private TModel _model;
        /// <summary>
        /// Gets or sets the underlying <typeparamref name="TModel"/> object.
        /// </summary>
        public TModel Model
        {
            get => _model;
            set
            {
                if (!EqualityComparer<TModel>.Default.Equals(_model, value))
                {
                    _model = value;

                    // Raise the PropertyChanged event for all properties.
                    OnPropertyChanged(string.Empty);
                }
            }
        }

        public bool Equals(TModel other)
            => EqualityComparer<TModel>.Default.Equals(_model, other);

        public bool Equals(ViewModel<TModel> other)
            => other is not null && EqualityComparer<TModel>.Default.Equals(_model, other._model);
    }

    /// <summary>
    /// Base ViewModel implementation, contains methods to
    /// handle property changes.
    /// </summary>
    public abstract class ViewModel : INotifyPropertyChanged
    {
        private readonly object _eventLock = new();
        private readonly Dictionary<PropertyChangedEventHandler, SynchronizationContext> PropertyChangedEvents = new();

        /// <summary>
        /// Whether or not to send property change notifications.
        /// </summary>
        protected bool NotifyPropertyChanges = true;

        /// <summary> 
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                if (value == null) return;
                lock (_eventLock)
                    PropertyChangedEvents[value] = SynchronizationContext.Current;
            }
            remove
            {
                if (value == null) return;
                lock (_eventLock)
                    _ = PropertyChangedEvents.Remove(value);
            }
        }

        /// <summary>
        /// Notifies listeners that a property value has changed.
        /// </summary>
        /// <param name="propertyName">Name of the property used to notify listeners. This
        /// value is optional and can be provided automatically when invoked from compilers
        /// that support <see cref="CallerMemberNameAttribute"/>.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (!NotifyPropertyChanges)
                return;

            KeyValuePair<PropertyChangedEventHandler, SynchronizationContext>[] snapshot;
            lock (_eventLock)
            {
                if (PropertyChangedEvents.Count == 0)
                    return;

                snapshot = PropertyChangedEvents.ToArray();
            }

            PropertyChangedEventArgs args = new PropertyChangedEventArgs(propertyName);
            foreach (var entry in snapshot)
            {
                var handler = entry.Key;
                var context = entry.Value;
                if (context == null || context == SynchronizationContext.Current)
                    handler.Invoke(this, args);
                else
                    context.Post(s => handler.Invoke(s, args), this);
            }
        }

        /// <summary>
        /// Checks if a property already matches a desired value. Sets the property and
        /// notifies listeners only when necessary.
        /// </summary>
        /// <typeparam name="T">Type of the property.</typeparam>
        /// <param name="storage">Reference to a property with both getter and setter.</param>
        /// <param name="value">Desired value for the property.</param>
        /// <param name="propertyName">Name of the property used to notify listeners. This
        /// value is optional and can be provided automatically when invoked from compilers that
        /// support CallerMemberName.</param>
        /// <returns>True if the value was changed, false if the existing value matched the
        /// desired value.</returns>
        protected bool Set<T>(ref T storage, T value,
            [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;

            storage = value;

            if (NotifyPropertyChanges)
                OnPropertyChanged(propertyName);

            return true;
        }
    }
}
