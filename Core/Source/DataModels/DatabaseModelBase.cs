using Jamiras.Components;
using Jamiras.Database;
using Jamiras.DataModels.Metadata;
using System.Collections.Generic;

namespace Jamiras.DataModels
{
    /// <summary>
    /// The base class for models that can be queried from a database.
    /// </summary>
    public abstract class DatabaseModelBase<T> : DataModelBase
        where T : DataModelBase, new()
    {
        private Dictionary<int, DataModelBase> _relatedModels = null;

        /// <summary>
        /// Constructs a <see cref="FluentQueryBuilder{T}"/> with the specified filter.
        /// </summary>
        /// <param name="property">Property to filter on.</param>
        /// <param name="value">Value to filter on.</param>
        /// <returns><see cref="FluentQueryBuilder{T}"/> for building the query.</returns>
        public static FluentQueryBuilder<T> Where(ModelProperty property, int value)
        {
            return new FluentQueryBuilder<T>().Where(property, value);
        }

        /// <summary>
        /// Constructs a <see cref="FluentQueryBuilder{T}"/> with the specified filter.
        /// </summary>
        /// <param name="property">Property to filter on.</param>
        /// <param name="value">Value to filter on.</param>
        /// <returns><see cref="FluentQueryBuilder{T}"/> for building the query.</returns>
        public static FluentQueryBuilder<T> Where(ModelProperty property, string value)
        {
            return new FluentQueryBuilder<T>().Where(property, value);
        }

        /// <summary>
        /// Gets the model with the specified primary key.
        /// </summary>
        /// <param name="id">Unique identifier of the model.</param>
        /// <returns>Found model, <c>null</c> if not found.</returns>
        public static T Get(int id)
        {
            var builder = new FluentQueryBuilder<T>();
            return builder.Where(builder.Metadata.PrimaryKeyProperty, id).First();
        }

        /// <summary>
        /// Writes any modifications made to the model to the database.
        /// </summary>
        /// <returns><c>true</c> if modifications where written, <c>false</c> if not.</returns>
        public bool Commit()
        {
            var database = ServiceRepository.Instance.FindService<IDatabase>();
            var metadata = (DatabaseModelMetadata)ServiceRepository.Instance.FindService<IDataModelMetadataRepository>().GetModelMetadata(typeof(T));
            return metadata.Commit(this, database);
        }

        protected T GetRelatedModel<T>(ModelProperty foriegnKeyProperty)
            where T : DataModelBase, new()
        {
            _relatedModels ??= new Dictionary<int, DataModelBase>();

            DataModelBase model;
            if (!_relatedModels.TryGetValue(foriegnKeyProperty.Key, out model))
            {
                var id = (int)GetValue(foriegnKeyProperty);

                var builder = new FluentQueryBuilder<T>();
                model = builder.Where(builder.Metadata.PrimaryKeyProperty, id).First();

                _relatedModels[foriegnKeyProperty.Key] = model;
            }

            return model as T;
        }
    }
}
