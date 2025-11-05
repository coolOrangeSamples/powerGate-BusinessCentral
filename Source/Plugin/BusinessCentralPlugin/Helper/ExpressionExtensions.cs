using System;
using System.Linq;
using powerGateServer.SDK;

namespace BusinessCentralPlugin.Helper
{
    public static class ExpressionExtensions
    {
        public static bool IsSimpleWhereToken<T>(this IExpression<T> expression)
        {
            var type = Type.GetType(
                "powerGateServer.Core.WcfFramework.Expressions.Where.SingleWhereClause`1,powerGateServer.Core");
            if (type == null) return false;
            var simpleWhereTokenType = type.MakeGenericType(typeof(T));
            return simpleWhereTokenType.IsInstanceOfType(expression.Where);
        }

        public static object GetWhereValueByName<T>(this IExpression<T> expression, string name)
        {
            var token = expression.Where.FirstOrDefault(w => w.PropertyName.Equals(name) && w.Value != null);
            if (token != null)
                return token.Value;

            return null;
        }
    }
}