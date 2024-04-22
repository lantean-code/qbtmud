using MudBlazor;
using System.Linq.Expressions;
using System.Reflection;

namespace Lantean.QBTMudBlade.Filter
{
    public record PropertyFilterDefinition<T>
    {
        public PropertyFilterDefinition(string column, string @operator, object? value)
        {
            var (expression, propertyType) = ExpressionModifier.CreatePropertySelector<T>(column);

            Column = column;
            ColumnType = propertyType;
            Operator = @operator;
            Value = value;
            Expression = expression;
        }

        public string Column { get; }

        public Type ColumnType { get; }

        public string Operator { get; set; }

        public object? Value { get; set; }

        public Expression<Func<T, object?>> Expression { get; }
    }


}
