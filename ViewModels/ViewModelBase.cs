using System.Collections.Generic;
using System.Linq;
using Jamiras.DataModels;
using Jamiras.ViewModels.Converters;

namespace Jamiras.ViewModels
{
    public abstract class ViewModelBase : ModelBase
    {
        internal IEnumerable<ModelBinding> Bindings
        {
            get { return _bindings; }
        }
        private List<ModelBinding> _bindings;

        internal class ModelBinding
        {
            public ModelBase Source { get; set; }
            public ModelProperty SourceProperty { get; set; }
            public ModelProperty TargetProperty { get; set; }
            public IConverter Converter { get; set; }
        }

        internal int BindingModelPropertyKey { get; set; }

        /// <summary>
        /// Binds a property on a model to the view model.
        /// </summary>
        /// <param name="source">Source object.</param>
        /// <param name="sourceProperty">Source object property to bind.</param>
        /// <param name="targetProperty">View model property to bind.</param>
        /// <param name="converter">Maps bound data from the source property type to the target property type.</param>
        protected void Bind(ModelBase source, ModelProperty sourceProperty, ModelProperty targetProperty, IConverter converter = null)
        {
            if (!_bindings.Any(b => b.Source == source && b.SourceProperty == sourceProperty))
                source.AddPropertyChangedHandler(sourceProperty, OnSourcePropertyChanged);

            var binding = new ModelBinding
            {
                Source = source,
                SourceProperty = sourceProperty,
                TargetProperty = targetProperty,
                Converter = converter
            };
            _bindings.Add(binding);

            RefreshBinding(binding);
        }

        private void OnSourcePropertyChanged(object sender, ModelPropertyChangedEventArgs e)
        {
            if (e.Property.Key != BindingModelPropertyKey)
            {
                foreach (var binding in _bindings.Where(b => b.SourceProperty == e.Property))
                    RefreshBinding(binding);
            }
        }

        /// <summary>
        /// Updates all bound view model properties from the backing models.
        /// </summary>
        public void Refresh()
        {
            foreach (var binding in _bindings)
                RefreshBinding(binding);

            foreach (var nestedViewModel in GetNestedViewModels())
                nestedViewModel.Refresh();
        }

        private void RefreshBinding(ModelBinding binding)
        {
            var value = binding.Source.GetValue(binding.SourceProperty);
            if (binding.Converter != null)
                value = binding.Converter.Convert(value);

            BindingModelPropertyKey = binding.TargetProperty.Key;
            SetValue(binding.TargetProperty, value);
            BindingModelPropertyKey = -1;
        }

        protected virtual IEnumerable<ViewModelBase> GetNestedViewModels()
        {
            return new ViewModelBase[0];
        }
    }
}
