using System;

namespace Jamiras.Components
{
    /// <summary>
    /// Constructor <see cref="Attribute"/> for an implementation of an interface stored in the <see cref="ServiceRepository"/> that
    /// indicates an interfaces used by the constructor should be populated with the implementations registered in the repository.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor)]
    public class ImportingConstructorAttribute : Attribute
    {
    }
}
