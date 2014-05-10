using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jamiras.DataModels.Metadata
{
    [Flags]
    public enum FieldAttributes
    {
        None = 0,

        GeneratedByCreate = 0x40,
        RefreshAfterCommit = 0x80,
    }
}
