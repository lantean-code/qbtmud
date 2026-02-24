using AwesomeAssertions;
using Lantean.QBTMud.Filter;
using System.Linq.Expressions;

namespace Lantean.QBTMud.Test.Filter
{
    public sealed class ExpressionModifierTests
    {
        [Fact]
        public void GIVEN_PropertySelectorAndPredicate_WHEN_Modify_THEN_ShouldComposeExpression()
        {
            Expression<Func<FilterEntity, string>> propertySelector = entity => entity.Name;
            Expression<Func<string, bool>> predicate = value => value.StartsWith("A");

            var result = propertySelector.Modify<FilterEntity>(predicate).Compile();

            result(new FilterEntity { Name = "Alpha" }).Should().BeTrue();
            result(new FilterEntity { Name = "Beta" }).Should().BeFalse();
        }

        [Fact]
        public void GIVEN_BinaryExpression_WHEN_ReplaceBinaryMatchesNodeType_THEN_ShouldReplaceOperation()
        {
            Expression<Func<FilterEntity, bool>> expression = entity => entity.IntValue == 5;

            var replaced = (Expression<Func<FilterEntity, bool>>)expression.ReplaceBinary(ExpressionType.Equal, ExpressionType.NotEqual);
            var compiled = replaced.Compile();

            compiled(new FilterEntity { IntValue = 5 }).Should().BeFalse();
            compiled(new FilterEntity { IntValue = 4 }).Should().BeTrue();
        }

        [Fact]
        public void GIVEN_BinaryExpression_WHEN_ReplaceBinaryDoesNotMatchNodeType_THEN_ShouldLeaveOperationUnchanged()
        {
            Expression<Func<FilterEntity, bool>> expression = entity => entity.IntValue == 5;

            var replaced = (Expression<Func<FilterEntity, bool>>)expression.ReplaceBinary(ExpressionType.GreaterThan, ExpressionType.LessThan);
            var compiled = replaced.Compile();

            compiled(new FilterEntity { IntValue = 5 }).Should().BeTrue();
            compiled(new FilterEntity { IntValue = 4 }).Should().BeFalse();
        }

        [Fact]
        public void GIVEN_NullableExpression_WHEN_GenerateBinaryWithValue_THEN_ShouldCompareUsingConvertedConstant()
        {
            Expression<Func<FilterEntity, int?>> expression = entity => entity.NullableIntValue;

            var result = expression.GenerateBinary<FilterEntity>(ExpressionType.Equal, 7).Compile();

            result(new FilterEntity { NullableIntValue = 7 }).Should().BeTrue();
            result(new FilterEntity { NullableIntValue = 8 }).Should().BeFalse();
        }

        [Fact]
        public void GIVEN_NullableExpression_WHEN_GenerateBinaryWithNullValue_THEN_ShouldCompareAgainstNull()
        {
            Expression<Func<FilterEntity, int?>> expression = entity => entity.NullableIntValue;

            var result = expression.GenerateBinary<FilterEntity>(ExpressionType.Equal, null).Compile();

            result(new FilterEntity { NullableIntValue = null }).Should().BeTrue();
            result(new FilterEntity { NullableIntValue = 1 }).Should().BeFalse();
        }

        [Fact]
        public void GIVEN_NonNullableExpressionAndNullValue_WHEN_GenerateBinary_THEN_ShouldReturnAlwaysTrue()
        {
            Expression<Func<FilterEntity, int>> expression = entity => entity.IntValue;

            var result = expression.GenerateBinary<FilterEntity>(ExpressionType.Equal, null).Compile();

            result(new FilterEntity { IntValue = 0 }).Should().BeTrue();
            result(new FilterEntity { IntValue = 10 }).Should().BeTrue();
        }

        [Fact]
        public void GIVEN_NonNullableExpressionAndValue_WHEN_GenerateBinary_THEN_ShouldReturnBinaryPredicate()
        {
            Expression<Func<FilterEntity, int>> expression = entity => entity.IntValue;

            var result = expression.GenerateBinary<FilterEntity>(ExpressionType.GreaterThan, 5).Compile();

            result(new FilterEntity { IntValue = 6 }).Should().BeTrue();
            result(new FilterEntity { IntValue = 5 }).Should().BeFalse();
        }

        [Fact]
        public void GIVEN_ExpressionReturnTypeAlreadyMatches_WHEN_ChangeExpressionReturnType_THEN_ShouldWrapBodyInConvert()
        {
            Expression<Func<FilterEntity, int>> expression = entity => entity.IntValue;

            var result = expression.ChangeExpressionReturnType<FilterEntity, int>();

            result.Body.NodeType.Should().Be(ExpressionType.Convert);
            result.Compile()(new FilterEntity { IntValue = 3 }).Should().Be(3);
        }

        [Fact]
        public void GIVEN_ObjectReturnType_WHEN_ChangeExpressionReturnType_THEN_ShouldThrowArgumentException()
        {
            Expression<Func<FilterEntity, int>> expression = entity => entity.IntValue;

            var action = () => expression.ChangeExpressionReturnType<FilterEntity, object?>();

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GIVEN_ExpressionReturnTypeDiffers_WHEN_ChangeExpressionReturnType_THEN_ShouldConvertToTargetType()
        {
            Expression<Func<FilterEntity, int>> expression = entity => entity.IntValue;

            var result = expression.ChangeExpressionReturnType<FilterEntity, long>();

            result.Body.NodeType.Should().Be(ExpressionType.Convert);
            result.Compile()(new FilterEntity { IntValue = 3 }).Should().Be(3L);
        }

        [Fact]
        public void GIVEN_ExistingPropertyName_WHEN_CreatePropertySelector_THEN_ShouldReturnSelectorAndPropertyType()
        {
            var (selector, type) = ExpressionModifier.CreatePropertySelector<FilterEntity>("Name");

            selector.Compile()(new FilterEntity { Name = "Name" }).Should().Be("Name");
            type.Should().Be(typeof(string));
        }

        [Fact]
        public void GIVEN_MissingPropertyName_WHEN_CreatePropertySelector_THEN_ShouldThrowInvalidOperationException()
        {
            var action = () => ExpressionModifier.CreatePropertySelector<FilterEntity>("Missing");

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("Unable to match property Missing for FilterEntity");
        }

        [Fact]
        public void GIVEN_ExpressionReplacer_WHEN_VisitedNodeMatchesFrom_THEN_ShouldReturnToExpression()
        {
            var from = Expression.Parameter(typeof(int), "value");
            var to = Expression.Constant(10);
            var target = new ExpressionReplacer(from, to);

            var result = target.Visit(from);

            result.Should().BeSameAs(to);
        }

        [Fact]
        public void GIVEN_ExpressionReplacer_WHEN_VisitedNodeDoesNotMatchFrom_THEN_ShouldReturnVisitedBaseResult()
        {
            var from = Expression.Parameter(typeof(int), "value");
            var to = Expression.Constant(10);
            var target = new ExpressionReplacer(from, to);
            var other = Expression.Constant(3);

            var result = target.Visit(other);

            result.Should().NotBeNull();
            result.Should().BeSameAs(other);
        }

        [Fact]
        public void GIVEN_ExpressionBodyIdentifier_WHEN_IdentifyCalledWithLambda_THEN_ShouldReturnBody()
        {
            Expression<Func<FilterEntity, int>> expression = entity => entity.IntValue;
            var target = new ExpressionBodyIdentifier();

            var result = target.Identify(expression);

            result.Should().BeSameAs(expression.Body);
        }

        [Fact]
        public void GIVEN_ExpressionParameterIdentifier_WHEN_IdentifyCalledWithLambda_THEN_ShouldReturnFirstParameter()
        {
            Expression<Func<FilterEntity, int>> expression = entity => entity.IntValue;
            var target = new ExpressionParameterIdentifier();

            var result = target.Identify(expression);

            result.Should().BeSameAs(expression.Parameters[0]);
        }

        [Fact]
        public void GIVEN_BinaryReplacer_WHEN_BinaryNodeMatchesFrom_THEN_ShouldReturnBinaryWithReplacementNodeType()
        {
            var target = new BinaryReplacer(ExpressionType.Equal, ExpressionType.NotEqual);
            var input = Expression.Equal(Expression.Constant(1), Expression.Constant(1));

            var result = (BinaryExpression)target.Visit(input)!;

            result.NodeType.Should().Be(ExpressionType.NotEqual);
        }

        [Fact]
        public void GIVEN_BinaryReplacer_WHEN_BinaryNodeDoesNotMatchFrom_THEN_ShouldKeepOriginalNodeType()
        {
            var target = new BinaryReplacer(ExpressionType.Equal, ExpressionType.NotEqual);
            var input = Expression.LessThan(Expression.Constant(1), Expression.Constant(2));

            var result = (BinaryExpression)target.Visit(input)!;

            result.NodeType.Should().Be(ExpressionType.LessThan);
        }

        private sealed class FilterEntity
        {
            public string Name { get; set; } = string.Empty;

            public int IntValue { get; set; }

            public int? NullableIntValue { get; set; }
        }
    }
}
