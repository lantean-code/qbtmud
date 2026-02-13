using AwesomeAssertions;
using Lantean.QBTMud.Filter;

namespace Lantean.QBTMud.Test.Filter
{
    public sealed class FilterExpressionGeneratorTests
    {
        [Fact]
        public void GIVEN_StringContains_WHEN_CaseInsensitive_THEN_ShouldMatchIgnoringCase()
        {
            var filter = new PropertyFilterDefinition<FilterEntity>("Name", FilterOperator.String.Contains, "abc");
            var expression = FilterExpressionGenerator.GenerateExpression(filter, false).Compile();

            expression(new FilterEntity { Name = "xxABCyy" }).Should().BeTrue();
        }

        [Fact]
        public void GIVEN_StringContains_WHEN_CaseSensitive_THEN_ShouldRespectCase()
        {
            var filter = new PropertyFilterDefinition<FilterEntity>("Name", FilterOperator.String.Contains, "abc");
            var expression = FilterExpressionGenerator.GenerateExpression(filter, true).Compile();

            expression(new FilterEntity { Name = "xxABCyy" }).Should().BeFalse();
        }

        [Fact]
        public void GIVEN_StringOperators_WHEN_EmptyAndNotEmpty_THEN_ShouldEvaluateWhitespaceCorrectly()
        {
            var emptyFilter = new PropertyFilterDefinition<FilterEntity>("Name", FilterOperator.String.Empty, null);
            var notEmptyFilter = new PropertyFilterDefinition<FilterEntity>("Name", FilterOperator.String.NotEmpty, null);
            Action emptyAction = () => FilterExpressionGenerator.GenerateExpression(emptyFilter, false);
            Action notEmptyAction = () => FilterExpressionGenerator.GenerateExpression(notEmptyFilter, false);

            emptyAction.Should().Throw<ArgumentException>();
            notEmptyAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GIVEN_StringFilterWithoutValue_WHEN_OperatorRequiresValue_THEN_ShouldReturnAlwaysTrue()
        {
            var filter = new PropertyFilterDefinition<FilterEntity>("Name", FilterOperator.String.StartsWith, null);
            var expression = FilterExpressionGenerator.GenerateExpression(filter, false).Compile();

            expression(new FilterEntity { Name = "anything" }).Should().BeTrue();
            expression(new FilterEntity { Name = null }).Should().BeTrue();
        }

        [Fact]
        public void GIVEN_StringOperators_WHEN_AllRemainingSupportedOperators_THEN_ShouldCoverBranches()
        {
            var notContainsFilter = new PropertyFilterDefinition<FilterEntity>("Name", FilterOperator.String.NotContains, "abc");
            var equalFilter = new PropertyFilterDefinition<FilterEntity>("Name", FilterOperator.String.Equal, "value");
            var notEqualFilter = new PropertyFilterDefinition<FilterEntity>("Name", FilterOperator.String.NotEqual, "value");
            var startsWithFilter = new PropertyFilterDefinition<FilterEntity>("Name", FilterOperator.String.StartsWith, "pre");
            var endsWithFilter = new PropertyFilterDefinition<FilterEntity>("Name", FilterOperator.String.EndsWith, "post");
            var unsupportedFilter = new PropertyFilterDefinition<FilterEntity>("Name", "unsupported", "value");

            var notContainsExpression = FilterExpressionGenerator.GenerateExpression(notContainsFilter, false).Compile();
            var equalExpression = FilterExpressionGenerator.GenerateExpression(equalFilter, false).Compile();
            var notEqualExpression = FilterExpressionGenerator.GenerateExpression(notEqualFilter, false).Compile();
            var startsWithExpression = FilterExpressionGenerator.GenerateExpression(startsWithFilter, false).Compile();
            var endsWithExpression = FilterExpressionGenerator.GenerateExpression(endsWithFilter, false).Compile();
            var unsupportedExpression = FilterExpressionGenerator.GenerateExpression(unsupportedFilter, false).Compile();

            notContainsExpression(new FilterEntity { Name = "zzz" }).Should().BeTrue();
            equalExpression(new FilterEntity { Name = "VALUE" }).Should().BeTrue();
            notEqualExpression(new FilterEntity { Name = "other" }).Should().BeTrue();
            startsWithExpression(new FilterEntity { Name = "prefix" }).Should().BeTrue();
            endsWithExpression(new FilterEntity { Name = "mypost" }).Should().BeTrue();
            unsupportedExpression(new FilterEntity { Name = "anything" }).Should().BeTrue();
        }

        [Fact]
        public void GIVEN_NumberOperators_WHEN_Compared_THEN_ShouldEvaluateNumericBranches()
        {
            var greaterThanFilter = new PropertyFilterDefinition<FilterEntity>("Size", FilterOperator.Number.GreaterThan, 10);
            var emptyFilter = new PropertyFilterDefinition<FilterEntity>("Size", FilterOperator.Number.Empty, null);
            var unsupportedFilter = new PropertyFilterDefinition<FilterEntity>("Size", "unsupported", 10);
            Action greaterThanAction = () => FilterExpressionGenerator.GenerateExpression(greaterThanFilter, false);

            var emptyExpression = FilterExpressionGenerator.GenerateExpression(emptyFilter, false).Compile();
            var unsupportedExpression = FilterExpressionGenerator.GenerateExpression(unsupportedFilter, false).Compile();

            greaterThanAction.Should().Throw<InvalidOperationException>();
            emptyExpression(new FilterEntity { Size = null }).Should().BeTrue();
            unsupportedExpression(new FilterEntity { Size = 1 }).Should().BeTrue();
        }

        [Fact]
        public void GIVEN_NumberOperators_WHEN_AllRemainingSupportedOperators_THEN_ShouldCoverBranches()
        {
            var equalFilter = new PropertyFilterDefinition<FilterEntity>("Size", FilterOperator.Number.Equal, 10);
            var notEqualFilter = new PropertyFilterDefinition<FilterEntity>("Size", FilterOperator.Number.NotEqual, 10);
            var greaterThanOrEqualFilter = new PropertyFilterDefinition<FilterEntity>("Size", FilterOperator.Number.GreaterThanOrEqual, 10);
            var lessThanFilter = new PropertyFilterDefinition<FilterEntity>("Size", FilterOperator.Number.LessThan, 10);
            var lessThanOrEqualFilter = new PropertyFilterDefinition<FilterEntity>("Size", FilterOperator.Number.LessThanOrEqual, 10);
            var notEmptyFilter = new PropertyFilterDefinition<FilterEntity>("Size", FilterOperator.Number.NotEmpty, null);

            var equalExpression = FilterExpressionGenerator.GenerateExpression(equalFilter, false).Compile();
            var notEqualExpression = FilterExpressionGenerator.GenerateExpression(notEqualFilter, false).Compile();
            var notEmptyExpression = FilterExpressionGenerator.GenerateExpression(notEmptyFilter, false).Compile();

            Action greaterThanOrEqualAction = () => FilterExpressionGenerator.GenerateExpression(greaterThanOrEqualFilter, false);
            Action lessThanAction = () => FilterExpressionGenerator.GenerateExpression(lessThanFilter, false);
            Action lessThanOrEqualAction = () => FilterExpressionGenerator.GenerateExpression(lessThanOrEqualFilter, false);

            equalExpression(new FilterEntity { Size = 10 }).Should().BeFalse();
            notEqualExpression(new FilterEntity { Size = 11 }).Should().BeTrue();
            notEmptyExpression(new FilterEntity { Size = 11 }).Should().BeTrue();
            greaterThanOrEqualAction.Should().Throw<InvalidOperationException>();
            lessThanAction.Should().Throw<InvalidOperationException>();
            lessThanOrEqualAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void GIVEN_NumberFilterWithoutValue_WHEN_OperatorRequiresValue_THEN_ShouldReturnAlwaysTrue()
        {
            var filter = new PropertyFilterDefinition<FilterEntity>("Size", FilterOperator.Number.GreaterThanOrEqual, null);
            var expression = FilterExpressionGenerator.GenerateExpression(filter, false).Compile();

            expression(new FilterEntity { Size = 0 }).Should().BeTrue();
        }

        [Fact]
        public void GIVEN_DateTimeOperators_WHEN_Compared_THEN_ShouldEvaluateDateBranches()
        {
            var value = new DateTime(2000, 01, 01, 00, 00, 00, DateTimeKind.Utc);
            var afterFilter = new PropertyFilterDefinition<FilterEntity>("AddedOn", FilterOperator.DateTime.After, value);
            var emptyFilter = new PropertyFilterDefinition<FilterEntity>("AddedOn", FilterOperator.DateTime.Empty, null);
            var unsupportedFilter = new PropertyFilterDefinition<FilterEntity>("AddedOn", "unsupported", value);
            Action afterAction = () => FilterExpressionGenerator.GenerateExpression(afterFilter, false);

            var emptyExpression = FilterExpressionGenerator.GenerateExpression(emptyFilter, false).Compile();
            var unsupportedExpression = FilterExpressionGenerator.GenerateExpression(unsupportedFilter, false).Compile();

            afterAction.Should().Throw<InvalidOperationException>();
            emptyExpression(new FilterEntity { AddedOn = null }).Should().BeTrue();
            unsupportedExpression(new FilterEntity { AddedOn = value }).Should().BeTrue();
        }

        [Fact]
        public void GIVEN_DateTimeOperators_WHEN_AllRemainingSupportedOperators_THEN_ShouldCoverBranches()
        {
            var value = new DateTime(2000, 01, 01, 00, 00, 00, DateTimeKind.Utc);
            var isFilter = new PropertyFilterDefinition<FilterEntity>("AddedOn", FilterOperator.DateTime.Is, value);
            var isNotFilter = new PropertyFilterDefinition<FilterEntity>("AddedOn", FilterOperator.DateTime.IsNot, value);
            var onOrAfterFilter = new PropertyFilterDefinition<FilterEntity>("AddedOn", FilterOperator.DateTime.OnOrAfter, value);
            var beforeFilter = new PropertyFilterDefinition<FilterEntity>("AddedOn", FilterOperator.DateTime.Before, value);
            var onOrBeforeFilter = new PropertyFilterDefinition<FilterEntity>("AddedOn", FilterOperator.DateTime.OnOrBefore, value);
            var notEmptyFilter = new PropertyFilterDefinition<FilterEntity>("AddedOn", FilterOperator.DateTime.NotEmpty, null);

            var isExpression = FilterExpressionGenerator.GenerateExpression(isFilter, false).Compile();
            var isNotExpression = FilterExpressionGenerator.GenerateExpression(isNotFilter, false).Compile();
            var notEmptyExpression = FilterExpressionGenerator.GenerateExpression(notEmptyFilter, false).Compile();

            Action onOrAfterAction = () => FilterExpressionGenerator.GenerateExpression(onOrAfterFilter, false);
            Action beforeAction = () => FilterExpressionGenerator.GenerateExpression(beforeFilter, false);
            Action onOrBeforeAction = () => FilterExpressionGenerator.GenerateExpression(onOrBeforeFilter, false);

            isExpression(new FilterEntity { AddedOn = value }).Should().BeFalse();
            isNotExpression(new FilterEntity { AddedOn = value.AddDays(1) }).Should().BeTrue();
            notEmptyExpression(new FilterEntity { AddedOn = value }).Should().BeTrue();
            onOrAfterAction.Should().Throw<InvalidOperationException>();
            beforeAction.Should().Throw<InvalidOperationException>();
            onOrBeforeAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void GIVEN_DateTimeValueMissing_WHEN_OperatorRequiresValue_THEN_ShouldReturnAlwaysTrue()
        {
            var filter = new PropertyFilterDefinition<FilterEntity>("AddedOn", FilterOperator.DateTime.Is, null);
            var expression = FilterExpressionGenerator.GenerateExpression(filter, false).Compile();

            expression(new FilterEntity { AddedOn = new DateTime(2000, 01, 01, 00, 00, 00, DateTimeKind.Utc) }).Should().BeTrue();
        }

        [Fact]
        public void GIVEN_BooleanOperators_WHEN_IsOperatorAndFallback_THEN_ShouldEvaluateExpected()
        {
            var isFilter = new PropertyFilterDefinition<FilterEntity>("Enabled", FilterOperator.Boolean.Is, true);
            var unsupportedFilter = new PropertyFilterDefinition<FilterEntity>("Enabled", "unsupported", true);
            var nullValueFilter = new PropertyFilterDefinition<FilterEntity>("Enabled", FilterOperator.Boolean.Is, null);

            var isExpression = FilterExpressionGenerator.GenerateExpression(isFilter, false).Compile();
            var unsupportedExpression = FilterExpressionGenerator.GenerateExpression(unsupportedFilter, false).Compile();
            var nullValueExpression = FilterExpressionGenerator.GenerateExpression(nullValueFilter, false).Compile();

            isExpression(new FilterEntity { Enabled = true }).Should().BeFalse();
            isExpression(new FilterEntity { Enabled = false }).Should().BeFalse();
            unsupportedExpression(new FilterEntity { Enabled = true }).Should().BeTrue();
            nullValueExpression(new FilterEntity { Enabled = false }).Should().BeTrue();
        }

        [Fact]
        public void GIVEN_EnumOperators_WHEN_IsAndIsNot_THEN_ShouldEvaluateExpected()
        {
            var isFilter = new PropertyFilterDefinition<FilterEntity>("Mode", FilterOperator.Enum.Is, FilterMode.Fast);
            var isNotFilter = new PropertyFilterDefinition<FilterEntity>("Mode", FilterOperator.Enum.IsNot, FilterMode.Slow);
            var unsupportedFilter = new PropertyFilterDefinition<FilterEntity>("Mode", "unsupported", FilterMode.Fast);

            var isExpression = FilterExpressionGenerator.GenerateExpression(isFilter, false).Compile();
            var isNotExpression = FilterExpressionGenerator.GenerateExpression(isNotFilter, false).Compile();
            var unsupportedExpression = FilterExpressionGenerator.GenerateExpression(unsupportedFilter, false).Compile();

            isExpression(new FilterEntity { Mode = FilterMode.Fast }).Should().BeFalse();
            isNotExpression(new FilterEntity { Mode = FilterMode.Fast }).Should().BeTrue();
            unsupportedExpression(new FilterEntity { Mode = FilterMode.Slow }).Should().BeTrue();
        }

        [Fact]
        public void GIVEN_EnumOperatorValueNull_WHEN_Generated_THEN_ShouldReturnAlwaysTrue()
        {
            var nullValueFilter = new PropertyFilterDefinition<FilterEntity>("Mode", FilterOperator.Enum.Is, null);
            var expression = FilterExpressionGenerator.GenerateExpression(nullValueFilter, false).Compile();

            expression(new FilterEntity { Mode = FilterMode.Slow }).Should().BeTrue();
        }

        [Fact]
        public void GIVEN_GuidOperators_WHEN_EqualNotEqualAndFallback_THEN_ShouldEvaluateExpected()
        {
            var id = Guid.NewGuid();
            var equalFilter = new PropertyFilterDefinition<FilterEntity>("Id", FilterOperator.Guid.Equal, id);
            var notEqualFilter = new PropertyFilterDefinition<FilterEntity>("Id", FilterOperator.Guid.NotEqual, id);
            var unsupportedFilter = new PropertyFilterDefinition<FilterEntity>("Id", "unsupported", id);
            var nullValueFilter = new PropertyFilterDefinition<FilterEntity>("Id", FilterOperator.Guid.Equal, null);

            var equalExpression = FilterExpressionGenerator.GenerateExpression(equalFilter, false).Compile();
            var notEqualExpression = FilterExpressionGenerator.GenerateExpression(notEqualFilter, false).Compile();
            var unsupportedExpression = FilterExpressionGenerator.GenerateExpression(unsupportedFilter, false).Compile();
            var nullValueExpression = FilterExpressionGenerator.GenerateExpression(nullValueFilter, false).Compile();

            equalExpression(new FilterEntity { Id = id }).Should().BeFalse();
            notEqualExpression(new FilterEntity { Id = Guid.NewGuid() }).Should().BeTrue();
            unsupportedExpression(new FilterEntity { Id = id }).Should().BeTrue();
            nullValueExpression(new FilterEntity { Id = id }).Should().BeTrue();
        }

        [Fact]
        public void GIVEN_UnsupportedFieldType_WHEN_FilterGenerated_THEN_ShouldReturnAlwaysTrue()
        {
            var filter = new PropertyFilterDefinition<FilterEntity>("Payload", "unsupported", "value");
            var expression = FilterExpressionGenerator.GenerateExpression(filter, false).Compile();

            expression(new FilterEntity { Payload = new FilterPayload() }).Should().BeTrue();
        }

        private enum FilterMode
        {
            Slow = 0,
            Fast = 1
        }

        private sealed class FilterEntity
        {
            public string? Name { get; set; }

            public int? Size { get; set; }

            public DateTime? AddedOn { get; set; }

            public bool Enabled { get; set; }

            public FilterMode Mode { get; set; }

            public Guid Id { get; set; }

            public FilterPayload? Payload { get; set; }
        }

        private sealed class FilterPayload
        {
        }
    }
}
