using System.Globalization;

namespace Lantean.QBTMud.Core.Filter
{
    internal static class FilterValueParser
    {
        internal static bool TryParse(Type type, object? value, out object? parsedValue)
        {
            var valueType = Nullable.GetUnderlyingType(type) ?? type;
            parsedValue = value;
            if (value is null)
            {
                return !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
            }

            if (valueType.IsInstanceOfType(value))
            {
                return true;
            }

            var text = Convert.ToString(value, CultureInfo.CurrentCulture);
            if (string.IsNullOrWhiteSpace(text) && Nullable.GetUnderlyingType(type) is not null)
            {
                parsedValue = null;
                return true;
            }

            if (valueType.IsEnum)
            {
                return Enum.TryParse(valueType, text, true, out parsedValue);
            }

            if (valueType == typeof(Guid))
            {
                return TryParseValue<Guid>(text, out parsedValue);
            }

            return Type.GetTypeCode(valueType) switch
            {
                TypeCode.Boolean => TryParseValue<bool>(text, out parsedValue),
                TypeCode.Byte => TryParseValue<byte>(text, out parsedValue),
                TypeCode.SByte => TryParseValue<sbyte>(text, out parsedValue),
                TypeCode.Int16 => TryParseValue<short>(text, out parsedValue),
                TypeCode.UInt16 => TryParseValue<ushort>(text, out parsedValue),
                TypeCode.Int32 => TryParseValue<int>(text, out parsedValue),
                TypeCode.UInt32 => TryParseValue<uint>(text, out parsedValue),
                TypeCode.Int64 => TryParseValue<long>(text, out parsedValue),
                TypeCode.UInt64 => TryParseValue<ulong>(text, out parsedValue),
                TypeCode.Single => TryParseValue<float>(text, out parsedValue),
                TypeCode.Double => TryParseValue<double>(text, out parsedValue),
                TypeCode.Decimal => TryParseValue<decimal>(text, out parsedValue),
                TypeCode.DateTime => TryParseValue<DateTime>(text, out parsedValue),
                TypeCode.String => AssignText(text, out parsedValue),
                _ => false
            };
        }

        private static bool TryParseValue<T>(string? text, out object? parsedValue) where T : IParsable<T>
        {
            var succeeded = T.TryParse(text, CultureInfo.CurrentCulture, out var value);
            parsedValue = value;
            return succeeded;
        }

        private static bool AssignText(string? text, out object? parsedValue)
        {
            parsedValue = text;
            return true;
        }
    }
}
