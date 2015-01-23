using System;
using System.Collections;
using Jamiras.DataModels.Metadata;

namespace Jamiras.DataModels
{
    internal interface IDataModelCollection : IEnumerable
    {
        void Add(DataModelBase item);

        bool Contains(DataModelBase item);

        Type ModelType { get; }
    }

    internal interface IDataModelCollectionMetadata
    {
        ModelMetadata ModelMetadata { get; }

        ModelProperty CollectionFilterKeyProperty { get; }
    }
}
