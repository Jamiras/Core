using Jamiras.Components;
using Jamiras.DataModels;

namespace Jamiras.ViewModels
{
    /// <summary>
    /// Base class for a model that can be bound to another model.
    /// </summary>
    public abstract class ViewModelBase : ModelBase
    {
        protected ViewModelBase()
        {
            _bindings = EmptyTinyDictionary<int, ModelBinding>.Instance;
        }

        private ITinyDictionary<int, ModelBinding> _bindings;
        private int _propertyBeingSynchronized;

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

            if (binding != null)
            {
                if (!IsObserving(binding.Source, binding.SourceProperty))
                    binding.Source.AddPropertyChangedHandler(binding.SourceProperty, OnSourcePropertyChanged);

                _bindings = _bindings.Add(viewModelProperty.Key, binding);

                RefreshBinding(viewModelProperty.Key, binding);
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
            _bindings.TryGetValue(viewModelProperty.Key, out binding);
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

        private void RefreshBinding(int localPropertyKey, ModelBinding binding)
        {
            object value;
            if (binding.TryPullValue(out value))
                SynchronizeValue(this, ModelProperty.GetPropertyForKey(localPropertyKey), value);
        }

        protected override void OnModelPropertyChanged(ModelPropertyChangedEventArgs e)
        {
            if (e.Property.Key != _propertyBeingSynchronized)
            {
                ModelBinding binding;
                if (_bindings.TryGetValue(e.Property.Key, out binding) && binding.Mode == ModelBindingMode.TwoWay)
                    PushValue(binding, e.Property, e.NewValue);
            }

            base.OnModelPropertyChanged(e);
        }

        internal virtual void PushValue(ModelBinding binding, ModelProperty localProperty, object value)
        {
            if (binding.Converter != null)
            {
                if (binding.Converter.ConvertBack(ref value) != null)
                    return;
            }

            SynchronizeValue(binding.Source, binding.SourceProperty, value);
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
        /// Commits any <see cref="BindingMode.Committed"/> bindings.
        /// </summary>
        public void Commit()
        {
            var compositeViewModel = this as ICompositeViewModel;
            if (compositeViewModel != null)
            {
                foreach (var child in compositeViewModel.GetChildren())
                    child.PushCommitBindings();
            }

            PushCommitBindings();
        }

        private void PushCommitBindings()
        {
            OnBeforeCommit();

            foreach (var kvp in _bindings)
            {
                if (kvp.Value.Mode == ModelBindingMode.Committed)
                {
                    var viewModelProperty = ModelProperty.GetPropertyForKey(kvp.Key);
                    var value = GetValue(viewModelProperty);
                    PushValue(kvp.Value, viewModelProperty, value);
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
