using AwesomeAssertions;
using MudBlazor;
using FilterOperator = Lantean.QBTMud.Filter.FilterOperator;

namespace Lantean.QBTMud.Test.Filter
{
    public sealed class FilterOperatorTests
    {
        [Fact]
        public void GIVEN_TypeString_WHEN_GetOperatorByDataType_THEN_ShouldReturnStringOperators()
        {
            var result = FilterOperator.GetOperatorByDataType(typeof(string));

            result.Should().Equal(
                FilterOperator.String.Contains,
                FilterOperator.String.NotContains,
                FilterOperator.String.Equal,
                FilterOperator.String.NotEqual,
                FilterOperator.String.StartsWith,
                FilterOperator.String.EndsWith,
                FilterOperator.String.Empty,
                FilterOperator.String.NotEmpty);
        }

        [Fact]
        public void GIVEN_FieldTypeIsString_WHEN_GetOperatorByDataType_THEN_ShouldReturnStringOperators()
        {
            var fieldType = new FieldType
            {
                IsString = true
            };

            var result = FilterOperator.GetOperatorByDataType(fieldType);

            result.Should().Equal(
                FilterOperator.String.Contains,
                FilterOperator.String.NotContains,
                FilterOperator.String.Equal,
                FilterOperator.String.NotEqual,
                FilterOperator.String.StartsWith,
                FilterOperator.String.EndsWith,
                FilterOperator.String.Empty,
                FilterOperator.String.NotEmpty);
        }

        [Fact]
        public void GIVEN_FieldTypeIsNumber_WHEN_GetOperatorByDataType_THEN_ShouldReturnNumberOperators()
        {
            var fieldType = new FieldType
            {
                IsNumber = true
            };

            var result = FilterOperator.GetOperatorByDataType(fieldType);

            result.Should().Equal(
                FilterOperator.Number.Equal,
                FilterOperator.Number.NotEqual,
                FilterOperator.Number.GreaterThan,
                FilterOperator.Number.GreaterThanOrEqual,
                FilterOperator.Number.LessThan,
                FilterOperator.Number.LessThanOrEqual,
                FilterOperator.Number.Empty,
                FilterOperator.Number.NotEmpty);
        }

        [Fact]
        public void GIVEN_FieldTypeIsEnum_WHEN_GetOperatorByDataType_THEN_ShouldReturnEnumOperators()
        {
            var fieldType = new FieldType
            {
                IsEnum = true
            };

            var result = FilterOperator.GetOperatorByDataType(fieldType);

            result.Should().Equal(
                FilterOperator.Enum.Is,
                FilterOperator.Enum.IsNot);
        }

        [Fact]
        public void GIVEN_FieldTypeIsBoolean_WHEN_GetOperatorByDataType_THEN_ShouldReturnBooleanOperators()
        {
            var fieldType = new FieldType
            {
                IsBoolean = true
            };

            var result = FilterOperator.GetOperatorByDataType(fieldType);

            result.Should().Equal(
                FilterOperator.Boolean.Is);
        }

        [Fact]
        public void GIVEN_FieldTypeIsDateTime_WHEN_GetOperatorByDataType_THEN_ShouldReturnDateTimeOperators()
        {
            var fieldType = new FieldType
            {
                IsDateTime = true
            };

            var result = FilterOperator.GetOperatorByDataType(fieldType);

            result.Should().Equal(
                FilterOperator.DateTime.Is,
                FilterOperator.DateTime.IsNot,
                FilterOperator.DateTime.After,
                FilterOperator.DateTime.OnOrAfter,
                FilterOperator.DateTime.Before,
                FilterOperator.DateTime.OnOrBefore,
                FilterOperator.DateTime.Empty,
                FilterOperator.DateTime.NotEmpty);
        }

        [Fact]
        public void GIVEN_FieldTypeIsGuid_WHEN_GetOperatorByDataType_THEN_ShouldReturnGuidOperators()
        {
            var fieldType = new FieldType
            {
                IsGuid = true
            };

            var result = FilterOperator.GetOperatorByDataType(fieldType);

            result.Should().Equal(
                FilterOperator.Guid.Equal,
                FilterOperator.Guid.NotEqual);
        }

        [Fact]
        public void GIVEN_FieldTypeUnknown_WHEN_GetOperatorByDataType_THEN_ShouldReturnEmptyOperators()
        {
            var fieldType = new FieldType();

            var result = FilterOperator.GetOperatorByDataType(fieldType);

            result.Should().BeEmpty();
        }
    }
}
