using MudBlazor;
using System.Linq.Expressions;

namespace Lantean.QBTMudBlade.Filter
{
    public static class FilterExpressionGenerator
    {
        public static Expression<Func<T, bool>> GenerateExpression<T>(PropertyFilterDefinition<T> filter, bool caseSensitive)
        {
            var propertyExpression = filter.Expression;

            if (propertyExpression is null)
            {
                return x => true;
            }

            var fieldType = FieldType.Identify(filter.ColumnType);

            if (fieldType.IsString)
            {
                var value = filter.Value?.ToString();

                if (value is null && filter.Operator != FilterOperator.String.Empty && filter.Operator != FilterOperator.String.NotEmpty)
                {
                    return x => true;
                }

                var stringComparer = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                return filter.Operator switch
                {
                    FilterOperator.String.Contains =>
                        propertyExpression.Modify<T>((Expression<Func<object?, bool>>)(x => (string?)x != null && value != null && ((string)x).Contains(value, stringComparer))),
                    FilterOperator.String.NotContains =>
                        propertyExpression.Modify<T>((Expression<Func<object?, bool>>)(x => (string?)x != null && value != null && !((string)x).Contains(value, stringComparer))),
                    FilterOperator.String.Equal =>
                        propertyExpression.Modify<T>((Expression<Func<object?, bool>>)(x => (string?)x != null && ((string)x).Equals(value, stringComparer))),
                    FilterOperator.String.NotEqual =>
                        propertyExpression.Modify<T>((Expression<Func<object?, bool>>)(x => (string?)x != null && !((string)x).Equals(value, stringComparer))),
                    FilterOperator.String.StartsWith =>
                        propertyExpression.Modify<T>((Expression<Func<object?, bool>>)(x => (string?)x != null && value != null && ((string)x).StartsWith(value, stringComparer))),
                    FilterOperator.String.EndsWith =>
                        propertyExpression.Modify<T>((Expression<Func<object?, bool>>)(x => (string?)x != null && value != null && ((string)x).EndsWith(value, stringComparer))),
                    FilterOperator.String.Empty => propertyExpression.Modify<T>((Expression<Func<string?, bool>>)(x => string.IsNullOrWhiteSpace(x))),
                    FilterOperator.String.NotEmpty => propertyExpression.Modify<T>((Expression<Func<string?, bool>>)(x => !string.IsNullOrWhiteSpace(x))),
                    _ => x => true
                };
            }

            if (fieldType.IsNumber)
            {
                if (filter.Value is null && filter.Operator != FilterOperator.Number.Empty && filter.Operator != FilterOperator.Number.NotEmpty)
                {
                    return x => true;
                }

                return filter.Operator switch
                {
                    FilterOperator.Number.Equal => propertyExpression.GenerateBinary<T>(ExpressionType.Equal, filter.Value),
                    FilterOperator.Number.NotEqual => propertyExpression.GenerateBinary<T>(ExpressionType.NotEqual, filter.Value),
                    FilterOperator.Number.GreaterThan => propertyExpression.GenerateBinary<T>(ExpressionType.GreaterThan, filter.Value),
                    FilterOperator.Number.GreaterThanOrEqual => propertyExpression.GenerateBinary<T>(ExpressionType.GreaterThanOrEqual, filter.Value),
                    FilterOperator.Number.LessThan => propertyExpression.GenerateBinary<T>(ExpressionType.LessThan, filter.Value),
                    FilterOperator.Number.LessThanOrEqual => propertyExpression.GenerateBinary<T>(ExpressionType.LessThanOrEqual, filter.Value),
                    FilterOperator.Number.Empty => propertyExpression.GenerateBinary<T>(ExpressionType.Equal, null),
                    FilterOperator.Number.NotEmpty => propertyExpression.GenerateBinary<T>(ExpressionType.NotEqual, null),
                    _ => x => true
                };
            }

            if (fieldType.IsDateTime)
            {
                if (filter.Value is null && filter.Operator != FilterOperator.DateTime.Empty && filter.Operator != FilterOperator.DateTime.NotEmpty)
                {
                    return x => true;
                }

                return filter.Operator switch
                {
                    FilterOperator.DateTime.Is => propertyExpression.GenerateBinary<T>(ExpressionType.Equal, filter.Value),
                    FilterOperator.DateTime.IsNot => propertyExpression.GenerateBinary<T>(ExpressionType.NotEqual, filter.Value),
                    FilterOperator.DateTime.After => propertyExpression.GenerateBinary<T>(ExpressionType.GreaterThan, filter.Value),
                    FilterOperator.DateTime.OnOrAfter => propertyExpression.GenerateBinary<T>(ExpressionType.GreaterThanOrEqual, filter.Value),
                    FilterOperator.DateTime.Before => propertyExpression.GenerateBinary<T>(ExpressionType.LessThan, filter.Value),
                    FilterOperator.DateTime.OnOrBefore => propertyExpression.GenerateBinary<T>(ExpressionType.LessThanOrEqual, filter.Value),
                    FilterOperator.DateTime.Empty => propertyExpression.GenerateBinary<T>(ExpressionType.Equal, null),
                    FilterOperator.DateTime.NotEmpty => propertyExpression.GenerateBinary<T>(ExpressionType.NotEqual, null),
                    _ => x => true
                };
            }

            if (fieldType.IsBoolean)
            {
                if (filter.Value is null)
                {
                    return x => true;
                }

                return filter.Operator switch
                {
                    FilterOperator.Boolean.Is => propertyExpression.GenerateBinary<T>(ExpressionType.Equal, filter.Value),
                    _ => x => true
                };
            }

            if (fieldType.IsEnum)
            {
                if (filter.Value is null)
                {
                    return x => true;
                }

                return filter.Operator switch
                {
                    FilterOperator.Enum.Is => propertyExpression.GenerateBinary<T>(ExpressionType.Equal, filter.Value),
                    FilterOperator.Enum.IsNot => propertyExpression.GenerateBinary<T>(ExpressionType.NotEqual, filter.Value),
                    _ => x => true
                };
            }

            if (fieldType.IsGuid)
            {
                return filter.Operator switch
                {
                    FilterOperator.Guid.Equal => propertyExpression.GenerateBinary<T>(ExpressionType.Equal, filter.Value),
                    FilterOperator.Guid.NotEqual => propertyExpression.GenerateBinary<T>(ExpressionType.NotEqual, filter.Value),
                    _ => x => true
                };
            }

            return x => true;
        }
    }
}