using System;
using System.Collections;
using Jamiras.Database;
using Jamiras.DataModels.Metadata;

namespace Jamiras.DataModels
{
    internal interface IDataModelCollection : IEnumerable
    {
        void Add(DataModelBase item);

        bool Contains(DataModelBase item);

        int Count { get; }

        Type ModelType { get; }
    }

    internal interface IDataModelCollectionMetadata
    {
        ModelMetadata ModelMetadata { get; }

        ModelProperty CollectionFilterKeyProperty { get; }

        bool Query(ModelBase model, int maxResults, object primaryKey, IDatabase database);
    }
}
