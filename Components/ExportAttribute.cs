using System;

namespace Jamiras.Components
{
    /// <summary>
    /// Class-level <see cref="Attribute"/> for an implementation of an interface stored in the <see cref="ServiceRepository"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ExportAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExportAttribute"/> class.
        /// </summary>
        /// <param name="exportedInterface">The exported interface that the attached class implements.</param>
        public ExportAttribute(Type exportedInterface)
        {
            ExportedInterface = exportedInterface;
        }

        /// <summary>
        /// Gets the exported interface implemented by the attached class.
        /// </summary>
        public Type ExportedInterface { get; private set; }
    }
}
