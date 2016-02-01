using System;

namespace Jamiras.DataModels.Metadata
{
    [Flags]
    public enum FieldAttributes
    {
        None = 0,
        Required = (int)InternalFieldAttributes.Required,
    }

    [Flags]
    internal enum InternalFieldAttributes
    {
        None = 0,
        PrimaryKey = 0x01,
        Required = 0x02,

        Custom1 = 0x10,

        GeneratedByCreate = 0x40,
        RefreshAfterCommit = 0x80,
    }
}
