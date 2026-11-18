using System.Reflection;
using Lantean.QBTMud.Core.Filter;
using Lantean.QBTMud.Core.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using FilterOperator = Lantean.QBTMud.Core.Filter.FilterOperator;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class FilterOptionsDialog<T>
    {
        private static readonly IReadOnlyList<PropertyInfo> _properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        private List<PropertyFilterDefinition<T>> _workingDefinitions = [];
        private List<PropertyFilterDefinition<T>>? _sourceDefinitions;
        private bool _definitionsInitialized;

        protected string? ValidationError { get; private set; }

        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; } = default!;

        protected IReadOnlyList<PropertyInfo> Columns => _properties;

        [Parameter]
        public List<PropertyFilterDefinition<T>>? FilterDefinitions { get; set; }

        protected override void OnParametersSet()
        {
            if (_definitionsInitialized && ReferenceEquals(_sourceDefinitions, FilterDefinitions))
            {
                return;
            }

            _definitionsInitialized = true;
            _sourceDefinitions = FilterDefinitions;
            _workingDefinitions = FilterDefinitions is null
                ? new List<PropertyFilterDefinition<T>>()
                : FilterDefinitions.Select(def => new PropertyFilterDefinition<T>(def.Column, def.Operator, def.Value)).ToList();
        }

        protected void RemoveDefinition(PropertyFilterDefinition<T> definition)
        {
            _workingDefinitions.Remove(definition);
            ValidationError = null;
        }

        protected void DefinitionOperatorChanged(PropertyFilterDefinition<T> definition, string @operator)
        {
            definition.Operator = @operator;
            ValidationError = null;
        }

        protected void DefinitionValueChanged(PropertyFilterDefinition<T> definition, object? value)
        {
            definition.Value = value;
            ValidationError = null;
        }

        protected string? Column { get; set; }
        protected Type? ColumnType { get; set; }
        protected string? Operator { get; set; }
        protected string? Value { get; set; }

        protected void ColumnChanged(string column)
        {
            Column = column;
            ColumnType = _properties.FirstOrDefault(p => p.Name == column)?.PropertyType;
            Operator = null;
            Value = null;
            ValidationError = null;
        }

        protected IEnumerable<string> GetAvailablePropertyNames()
        {
            foreach (var propertyName in _properties.Select(p => p.Name))
            {
                if (!_workingDefinitions.Exists(d => d.Column == propertyName))
                {
                    yield return propertyName;
                }
            }
        }

        protected void OperatorChanged(string @operator)
        {
            Operator = @operator;
            ValidationError = null;
        }

        protected void ValueChanged(string value)
        {
            Value = value;
            ValidationError = null;
        }

        protected async Task AddDefinition()
        {
            if (Column is null || Operator is null || _workingDefinitions.Exists(d => d.Column == Column))
            {
                return;
            }

            CreateAndAdd(Column, Operator, Value);

            await InvokeAsync(StateHasChanged);
        }

        private bool CreateAndAdd(string column, string @operator, object? value)
        {
            var definition = new PropertyFilterDefinition<T>(column, @operator, value);
            if (!TryNormaliseValue(definition))
            {
                return false;
            }

            _workingDefinitions.Add(definition);

            Column = null;
            Operator = null;
            Value = null;
            return true;
        }

        private bool TryNormaliseValue(PropertyFilterDefinition<T> definition)
        {
            if (definition.Operator == FilterOperator.String.Empty || definition.Operator == FilterOperator.String.NotEmpty)
            {
                return true;
            }

            if (definition.Value is not null && FilterValueParser.TryParse(definition.ColumnType, definition.Value, out var parsedValue) && parsedValue is not null)
            {
                definition.Value = parsedValue;
                ValidationError = null;
                return true;
            }

            ValidationError = LanguageLocalizer.Translate("AppFilterOptionsDialog", "Enter a valid filter value.");
            return false;
        }

        protected void Cancel()
        {
            MudDialog.Cancel();
        }

        protected void Submit()
        {
            if (Column is not null && Operator is not null && !_workingDefinitions.Exists(d => d.Column == Column))
            {
                if (!CreateAndAdd(Column, Operator, Value))
                {
                    return;
                }
            }

            foreach (var definition in _workingDefinitions)
            {
                if (!TryNormaliseValue(definition))
                {
                    return;
                }
            }

            MudDialog.Close(DialogResult.Ok(_workingDefinitions));
        }

        protected override Task Submit(KeyboardEvent keyboardEvent)
        {
            Submit();

            return Task.CompletedTask;
        }
    }
}
