using System;
using System.ComponentModel;
using Jamiras.Components;
using Jamiras.DataModels;
using System.Collections;

namespace Jamiras.ViewModels
{
    /// <summary>
    /// Base class for a model that can be bound to another model.
    /// </summary>
    public abstract class ViewModelBase : ModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ViewModelBase"/> class.
        /// </summary>
        protected ViewModelBase()
        {
            _bindings = EmptyTinyDictionary<int, ModelBinding>.Instance;
            _selfBindings = EmptyTinyDictionary<int, ModelBinding>.Instance;
        }

        private ITinyDictionary<int, ModelBinding> _bindings;
        private ITinyDictionary<int, ModelBinding> _selfBindings;
        private int _propertyBeingSynchronized;

        /// <summary>
        /// Gets the value of a <see cref="ModelProperty"/> for this instance.
        /// </summary>
        /// <param name="property">The <see cref="ModelProperty"/> to query.</param>
        /// <returns>The current value of the <see cref="ModelProperty"/> for this instance.</returns>
        public override sealed object GetValue(ModelProperty property)
        {
            return base.GetValue(property);
        }

        /// <summary>
        /// Sets the value of a <see cref="ModelProperty"/> for this instance.
        /// </summary>
        /// <param name="property">The <see cref="ModelProperty"/> to update.</param>
        /// <param name="value">The new value for the <see cref="ModelProperty"/>.</param>
        public override sealed void SetValue(ModelProperty property, object value)
        {
            base.SetValue(property, value);
        }

        /// <summary>
        /// Binds a property on a model to the view model.
        /// </summary>
        /// <param name="viewModelProperty">View model property to bind.</param>
        /// <param name="binding">Information about how the view model property is bound.</param>
        public void SetBinding(ModelProperty viewModelProperty, ModelBinding binding)
        {
            ModelBinding oldBinding;
            if (_bindings.TryGetValue(viewModelProperty.Key, out oldBinding))
            {
                _bindings = _bindings.Remove(viewModelProperty.Key);
                if (!IsObserving(oldBinding.Source, oldBinding.SourceProperty))
                    oldBinding.Source.RemovePropertyChangedHandler(oldBinding.SourceProperty, OnSourcePropertyChanged);
            }
            else if (_selfBindings.TryGetValue(viewModelProperty.Key, out oldBinding))
            {
                _selfBindings = _selfBindings.Remove(viewModelProperty.Key);
            }

            if (binding != null)
            {
                if (ReferenceEquals(binding.Source, this))
                {
                    _selfBindings = _selfBindings.Add(viewModelProperty.Key, binding);
                    RefreshBinding(viewModelProperty.Key, binding);
                }
                else
                {
                    if (!IsObserving(binding.Source, binding.SourceProperty))
                        binding.Source.AddPropertyChangedHandler(binding.SourceProperty, OnSourcePropertyChanged);

                    _bindings = _bindings.Add(viewModelProperty.Key, binding);
                    RefreshBinding(viewModelProperty.Key, binding);
                }
            }
        }

        /// <summary>
        /// Gets the binding associated to a property.
        /// </summary>
        /// <param name="viewModelProperty">Property to get binding for.</param>
        /// <returns>Requested binding, <c>null</c> if not bound.</returns>
        public ModelBinding GetBinding(ModelProperty viewModelProperty)
        {
            ModelBinding binding;
            if (!_bindings.TryGetValue(viewModelProperty.Key, out binding))
                _selfBindings.TryGetValue(viewModelProperty.Key, out binding);

            return binding;
        }

        private bool IsObserving(ModelBase model, ModelProperty property)
        {
            foreach (var binding in _bindings.Values)
            {
                if (binding.SourceProperty == property && ReferenceEquals(binding.Source, model))
                    return true;
            }

            return false;
        }

        private void OnSourcePropertyChanged(object sender, ModelPropertyChangedEventArgs e)
        {
            if (e.Property.Key != _propertyBeingSynchronized)
            {
                foreach (var kvp in _bindings)
                {
                    if (kvp.Value.SourceProperty == e.Property)
                        RefreshBinding(kvp.Key, kvp.Value);
                }
            }
        }

        /// <summary>
        /// Updates all bound view model properties from the backing models.
        /// </summary>
        public virtual void Refresh()
        {
            RefreshBindings();

            var compositeViewModel = this as ICompositeViewModel;
            if (compositeViewModel != null)
            {
                foreach (var child in compositeViewModel.GetChildren())
                    child.RefreshBindings();
            }
        }

        private void RefreshBindings()
        {
            foreach (var kvp in _bindings)
                RefreshBinding(kvp.Key, kvp.Value);
        }

        internal virtual void RefreshBinding(int localPropertyKey, ModelBinding binding)
        {
            object value;
            if (binding.TryPullValue(out value))
                SynchronizeValue(this, ModelProperty.GetPropertyForKey(localPropertyKey), value);
        }

        /// <summary>
        /// Notifies any subscribers that the value of a <see cref="ModelProperty" /> has changed.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnModelPropertyChanged(ModelPropertyChangedEventArgs e)
        {
            HandleModelPropertyChanged(e);
            base.OnModelPropertyChanged(e);
        }

        internal void HandleModelPropertyChanged(ModelPropertyChangedEventArgs e)
        {
            ModelBinding binding;
            if (_bindings.TryGetValue(e.Property.Key, out binding) ||
                _selfBindings.TryGetValue(e.Property.Key, out binding))
            {
                if (binding.Mode != ModelBindingMode.OneWay)
                    HandleBoundPropertyChanged(binding, e, binding.Mode == ModelBindingMode.TwoWay);
            }
            else
            {
                HandleUnboundPropertyChanged(e);
            }

            // if any self bindings are dependant on the modified field, update them now
            foreach (var kvp in _selfBindings)
            {
                if (kvp.Value.SourceProperty == e.Property)
                    RefreshBinding(kvp.Key, kvp.Value);
            }
        }

        internal virtual void HandleUnboundPropertyChanged(ModelPropertyChangedEventArgs e)
        {
        }

        internal virtual void HandleBoundPropertyChanged(ModelBinding binding, ModelPropertyChangedEventArgs e, bool pushToSource)
        {
            object convertedValue = e.NewValue;
            if (binding.Converter != null && binding.Converter.ConvertBack(ref convertedValue) != null)
                return;

            if (pushToSource)
                SynchronizeValue(binding.Source, binding.SourceProperty, convertedValue);

            if (binding.Converter != null && binding.Converter.Convert(ref convertedValue) == null && !Equals(convertedValue, e.NewValue) && !ArrayEquals(convertedValue, e.NewValue))
            {
                SynchronizeValue(this, e.Property, convertedValue);

                if (!String.IsNullOrEmpty(e.Property.PropertyName))
                {
                    var action = new Action(() => OnPropertyChanged(new PropertyChangedEventArgs(e.Property.PropertyName)));
                    if (System.Windows.Application.Current != null && System.Windows.Application.Current.Dispatcher != null)
                        System.Windows.Application.Current.Dispatcher.BeginInvoke(action);
                    else
                        action();
                }
            }
        }

        private static bool ArrayEquals(object a, object b)
        {
            if (a is IEnumerable && b is IEnumerable)
            {
                var iterA = ((IEnumerable)a).GetEnumerator();
                var iterB = ((IEnumerable)b).GetEnumerator();

                while (iterA.MoveNext())
                {
                    if (!iterB.MoveNext())
                        return false;

                    if (!Equals(iterA.Current, iterB.Current))
                        return false;
                }

                if (!iterB.MoveNext())
                    return true;
            }

            return false;
        }

        internal void SynchronizeValue(ModelBase model, ModelProperty property, object newValue)
        {
            _propertyBeingSynchronized = property.Key;
            try
            {
                model.SetValue(property, newValue);
            }
            finally
            {
                _propertyBeingSynchronized = 0;
            }
        }

        /// <summary>
        /// Commits any <see cref="ModelBindingMode.Committed"/> bindings.
        /// </summary>
        public void Commit()
        {
            OnBeforeCommit();

            var compositeViewModel = this as ICompositeViewModel;
            if (compositeViewModel != null)
            {
                foreach (var child in compositeViewModel.GetChildren())
                    child.Commit();
            }

            PushCommitBindings();
        }

        private void PushCommitBindings()
        {
            foreach (var kvp in _bindings)
            {
                var binding = kvp.Value;
                if (binding.Mode == ModelBindingMode.Committed)
                {
                    object oldValue;
                    binding.TryPullValue(out oldValue);

                    var viewModelProperty = ModelProperty.GetPropertyForKey(kvp.Key);
                    var value = GetValue(viewModelProperty);

                    if (!Equals(oldValue, value))
                        HandleBoundPropertyChanged(binding, new ModelPropertyChangedEventArgs(viewModelProperty, oldValue, value), true);
                }
            }
        }

        /// <summary>
        /// Performs any data processing that needs to occur prior to committing.
        /// </summary>
        protected virtual void OnBeforeCommit()
        {
        }
    }
}
