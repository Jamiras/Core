using System;
using Jamiras.IO.Serialization;
using Jamiras.Components;

namespace Jamiras.DataModels.Metadata
{
    /// <summary>
    /// Converter for mapping a model data to a JSON field without a direct conversion.
    /// </summary>
    public class JsonRelatedFieldConverter : JsonFieldConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonRelatedFieldConverter"/> class.
        /// </summary>
        /// <param name="jsonFieldName">Name of the json field.</param>
        /// <param name="type">Type of the json field.</param>
        /// <param name="getValue">Method to retrieve the mapped data.</param>
        public JsonRelatedFieldConverter(string jsonFieldName, JsonFieldType type, Func<ModelBase, IDataModelSource, object> getValue)
            : base(jsonFieldName, null, null, type)
        {
            _getValue = getValue;
        }

        private readonly Func<ModelBase, IDataModelSource, object> _getValue;
        private static IDataModelSource _dataModelSource;

        /// <summary>
        /// Gets the value from the model.
        /// </summary>
        protected override object GetValue(ModelBase model)
        {
            if (_dataModelSource == null)
                _dataModelSource = ServiceRepository.Instance.FindService<IDataModelSource>();

            return _getValue(model, _dataModelSource);
        }
    }
}
