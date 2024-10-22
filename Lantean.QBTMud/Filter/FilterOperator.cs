using MudBlazor;

namespace Lantean.QBTMud.Filter
{
    public static class FilterOperator
    {
        public static class String
        {
            public const string Contains = "contains";
            public const string NotContains = "not contains";
            public const string Equal = "equals";
            public const string NotEqual = "not equals";
            public const string StartsWith = "starts with";
            public const string EndsWith = "ends with";
            public const string Empty = "is empty";
            public const string NotEmpty = "is not empty";
        }

        public static class Number
        {
            public const string Equal = "=";
            public const string NotEqual = "!=";
            public const string GreaterThan = ">";
            public const string GreaterThanOrEqual = ">=";
            public const string LessThan = "<";
            public const string LessThanOrEqual = "<=";
            public const string Empty = "is empty";
            public const string NotEmpty = "is not empty";
        }

        public static class Enum
        {
            public const string Is = "is";
            public const string IsNot = "is not";
        }

        public static class Boolean
        {
            public const string Is = "is";
        }

        public static class DateTime
        {
            public const string Is = "is";
            public const string IsNot = "is not";
            public const string After = "is after";
            public const string OnOrAfter = "is on or after";
            public const string Before = "is before";
            public const string OnOrBefore = "is on or before";
            public const string Empty = "is empty";
            public const string NotEmpty = "is not empty";
        }

        public static class Guid
        {
            public const string Equal = "equals";
            public const string NotEqual = "not equals";
        }

        internal static string[] GetOperatorByDataType(Type type)
        {
            var fieldType = FieldType.Identify(type);
            return GetOperatorByDataType(fieldType);
        }

        internal static string[] GetOperatorByDataType(FieldType fieldType)
        {
            if (fieldType.IsString)
            {
                return
                [
                    String.Contains,
                    String.NotContains,
                    String.Equal,
                    String.NotEqual,
                    String.StartsWith,
                    String.EndsWith,
                    String.Empty,
                    String.NotEmpty,
                ];
            }
            if (fieldType.IsNumber)
            {
                return
                [
                    Number.Equal,
                    Number.NotEqual,
                    Number.GreaterThan,
                    Number.GreaterThanOrEqual,
                    Number.LessThan,
                    Number.LessThanOrEqual,
                    Number.Empty,
                    Number.NotEmpty,
                ];
            }
            if (fieldType.IsEnum)
            {
                return
                [
                    Enum.Is,
                    Enum.IsNot,
                ];
            }
            if (fieldType.IsBoolean)
            {
                return
                [
                    Boolean.Is,
                ];
            }
            if (fieldType.IsDateTime)
            {
                return
                [
                    DateTime.Is,
                    DateTime.IsNot,
                    DateTime.After,
                    DateTime.OnOrAfter,
                    DateTime.Before,
                    DateTime.OnOrBefore,
                    DateTime.Empty,
                    DateTime.NotEmpty,
                ];
            }
            if (fieldType.IsGuid)
            {
                return
                [
                    Guid.Equal,
                    Guid.NotEqual,
                ];
            }

            // default
            return [];
        }
    }
}