using System;
using Jamiras.IO.Serialization;
using Jamiras.Components;

namespace Jamiras.DataModels.Metadata
{
    public class JsonRelatedFieldConverter : JsonFieldConverter
    {
        public JsonRelatedFieldConverter(string jsonFieldName, JsonFieldType type, Func<ModelBase, IDataModelSource, object> getValue)
            : base(jsonFieldName, null, null, type)
        {
            _getValue = getValue;
        }

        private readonly Func<ModelBase, IDataModelSource, object> _getValue;
        private static IDataModelSource _dataModelSource;

        protected override object GetValue(ModelBase model)
        {
            if (_dataModelSource == null)
                _dataModelSource = ServiceRepository.Instance.FindService<IDataModelSource>();

            return _getValue(model, _dataModelSource);
        }
    }
}
