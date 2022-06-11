using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Jamiras.Components
{
    /// <summary>
    /// Allows you to get the name of a property on an object
    /// </summary>
    public static class ExpressionExtensions
    {
        /// <summary>
        /// Gets the name of the property or method referenced in the lambda expression.
        /// </summary>
        /// <param name="expression">The expression to extract the name from.</param>
        /// <returns>The name of the property or method referenced in the lambda expression.</returns>
        public static string GetMemberName(this LambdaExpression expression)
        {
            return expression.GetMemberInfo().Name;
        }

        internal static MemberInfo GetMemberInfo(this LambdaExpression expression)
        {
            var body = expression.Body as MemberExpression;
            if (body == null)
            {
                var unaryExpression = expression.Body as UnaryExpression;
                if (unaryExpression != null)
                    body = unaryExpression.Operand as MemberExpression;
            }

            if (body == null)
                throw new ArgumentException("Cannot resolve property or method name from expression");

            return body.Member;
        }
    }
}
