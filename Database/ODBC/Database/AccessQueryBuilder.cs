using Jamiras.Components;
using System;
using System.Text;

namespace Jamiras.Database
{
    internal class AccessQueryBuilder : QueryBuilder
    {
        public AccessQueryBuilder(DatabaseSchema schema)
            :base(schema)
        {

        }

        protected override void AppendBoolean(StringBuilder builder, bool value)
        {
            builder.Append(value ? "YES" : "NO");
        }

        protected override void AppendDateTime(StringBuilder builder, DateTime value)
        {
            builder.AppendFormat("#{0}#", value);
        }

        protected override void AppendDate(StringBuilder builder, Date value)
        {
            builder.AppendFormat("#{0}/{1}/{2}#", value.Month, value.Day, value.Year);
        }
    }
}
