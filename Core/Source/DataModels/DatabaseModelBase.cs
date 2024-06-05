using Jamiras.DataModels.Database;

namespace Jamiras.DataModels
{
    /// <summary>
    /// The base class for models that can be queried from a database.
    /// </summary>
    public abstract class DatabaseModelBase<T> : DataModelBase
        where T : DataModelBase, new()
    {
        public static QueryBuilder<T> Where(ModelProperty property, int value)
        {
            return new QueryBuilder<T>().Where(property, value);
        }

        /// <summary>
        /// Gets the model with the specified primary key.
        /// </summary>
        /// <param name="id">Unique identifier of the model.</param>
        /// <returns>Found model, <c>null</c> if not found.</returns>
        public static T Get(int id)
        {
            var builder = new QueryBuilder<T>();
            return builder.Where(builder.Metadata.PrimaryKeyProperty, id).First();
        }
    }
}
