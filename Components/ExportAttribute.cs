using System;

namespace Jamiras.Components
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ExportAttribute : Attribute
    {
        public ExportAttribute(Type exportedInterface)
        {
            ExportedInterface = exportedInterface;
        }

        public Type ExportedInterface { get; private set; }
    }
}
