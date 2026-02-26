using AwesomeAssertions;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Test.Helpers
{
    public sealed class ColumnDefinitionHelperTests
    {
        [Fact]
        public void GIVEN_RowTemplateOverloadWithTdClass_WHEN_CreateColumnDefinition_THEN_AllOptionsApplied()
        {
            RenderFragment<RowContext<TestModel>> template = context => builder => builder.AddContent(0, context.GetValue());

            var column = ColumnDefinitionHelper.CreateColumnDefinition(
                "Header",
                (TestModel model) => model.Value,
                template,
                iconOnly: true,
                width: 111,
                tdClass: "custom-td",
                classFunc: model => model.CssClass,
                enabled: false,
                initialDirection: SortDirection.Descending,
                id: "header");

            var model = new TestModel
            {
                Value = "Value",
                CssClass = "dynamic-class",
            };

            column.Class.Should().Be("no-wrap custom-td");
            column.IconOnly.Should().BeTrue();
            column.Width.Should().Be(111);
            column.Enabled.Should().BeFalse();
            column.InitialDirection.Should().Be(SortDirection.Descending);
            column.Id.Should().Be("header");
            column.ClassFunc.Should().NotBeNull();
            column.ClassFunc!(model).Should().Be("dynamic-class");
            var rowContext = column.GetRowContext(model);
            rowContext.HeaderText.Should().Be("Header");
            rowContext.GetValue().Should().Be("Value");
        }

        [Fact]
        public void GIVEN_FormatterOverloadWithoutTdClass_WHEN_CreateColumnDefinition_THEN_DefaultNoWrapClassIsUsed()
        {
            var column = ColumnDefinitionHelper.CreateColumnDefinition(
                "Header",
                (TestModel model) => model.Value,
                model => model.Value!.ToUpperInvariant());

            var model = new TestModel
            {
                Value = "Value",
            };

            column.Class.Should().Be("no-wrap");
            column.Formatter.Should().NotBeNull();
            column.GetRowContext(model).GetValue().Should().Be("VALUE");
        }

        [Fact]
        public void GIVEN_FormatterOverloadWithTdClass_WHEN_CreateColumnDefinition_THEN_TdClassIsAppended()
        {
            var column = ColumnDefinitionHelper.CreateColumnDefinition(
                "Header",
                (TestModel model) => model.Value,
                model => model.Value!.ToUpperInvariant(),
                tdClass: "custom-td");

            column.Class.Should().Be("no-wrap custom-td");
        }

        private sealed class TestModel
        {
            public string? Value { get; set; }

            public string? CssClass { get; set; }
        }
    }
}
