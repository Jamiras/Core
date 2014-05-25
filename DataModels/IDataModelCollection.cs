using System;
using System.Collections;

namespace Jamiras.DataModels
{
    internal interface IDataModelCollection : IEnumerable
    {
        bool IsCollectionChanged { get; }

        void Add(DataModelBase item);

        Type ModelType { get; }
    }
}
