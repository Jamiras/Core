using System.Linq;

namespace Jamiras.DataModels
{
    public static class DataModelChangeTracker
    {
        /// <summary>
        /// Determines if the specified property is modified on a model.
        /// </summary>
        /// <param name="model">The model to examine.</param>
        /// <param name="property">The property to examine.</param>
        /// <returns><c>true</c> if the property has been modified on the model, <c>false</c> if not.</returns>
        public static bool IsPropertyModified(DataModelBase model, ModelProperty property)
        {
            return (model.IsModified && model.UpdatedPropertyKeys.Contains(property.Key));
        }

        /// <summary>
        /// Gets the unmodified value of a property on a model.
        /// </summary>
        /// <param name="model">The model to examine.</param>
        /// <param name="property">The property to examine.</param>
        /// <returns>The unmodified value of the property (which may be the current value of the property).</returns>
        public static object GetOriginalValue(DataModelBase model, ModelProperty property)
        {
            return model.GetOriginalValue(property);
        }
    }
}
