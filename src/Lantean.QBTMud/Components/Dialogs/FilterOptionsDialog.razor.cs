using Lantean.QBTMud.Filter;
using Lantean.QBTMud.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Reflection;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class FilterOptionsDialog<T>
    {
        private static readonly IReadOnlyList<PropertyInfo> _properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        private List<PropertyFilterDefinition<T>> _workingDefinitions = [];

        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; } = default!;

        protected IReadOnlyList<PropertyInfo> Columns => _properties;

        [Parameter]
        public List<PropertyFilterDefinition<T>>? FilterDefinitions { get; set; }

        protected override void OnParametersSet()
        {
            _workingDefinitions = FilterDefinitions is null
                ? new List<PropertyFilterDefinition<T>>()
                : FilterDefinitions.Select(def => new PropertyFilterDefinition<T>(def.Column, def.Operator, def.Value)).ToList();
        }

        protected void RemoveDefinition(PropertyFilterDefinition<T> definition)
        {
            if (_workingDefinitions is null)
            {
                return;
            }
            _workingDefinitions.Remove(definition);
        }

        protected void DefinitionOperatorChanged(PropertyFilterDefinition<T> definition, string @operator)
        {
            var existingDefinition = _workingDefinitions?.Find(d => d == definition);
            if (existingDefinition is null)
            {
                return;
            }

            existingDefinition.Operator = @operator;
        }

        protected void DefinitionValueChanged(PropertyFilterDefinition<T> definition, object? value)
        {
            var existingDefinition = _workingDefinitions?.Find(d => d == definition);
            if (existingDefinition is null)
            {
                return;
            }

            existingDefinition.Value = value;
        }

        protected string? Column { get; set; }
        protected Type? ColumnType { get; set; }
        protected string? Operator { get; set; }
        protected string? Value { get; set; }

        protected void ColumnChanged(string column)
        {
            Column = column;
            ColumnType = _properties.FirstOrDefault(p => p.Name == column)?.PropertyType;
        }

        protected IEnumerable<string> GetAvailablePropertyNames()
        {
            foreach (var propertyName in _properties.Select(p => p.Name))
            {
                if (!(FilterDefinitions?.Exists(d => d.Column == propertyName) ?? false))
                {
                    yield return propertyName;
                }
            }
        }

        protected void OperatorChanged(string @operator)
        {
            Operator = @operator;
        }

        protected void ValueChanged(string value)
        {
            Value = value;
        }

        protected async Task AddDefinition()
        {
            if (Column is null || Operator is null || (_workingDefinitions?.Exists(d => d.Column == Column) ?? false))
            {
                return;
            }

            CreateAndAdd(Column, Operator, Value);

            await InvokeAsync(StateHasChanged);
        }

        private void CreateAndAdd(string column, string @operator, object? value)
        {
            _workingDefinitions ??= [];
            _workingDefinitions.Add(new PropertyFilterDefinition<T>(column, @operator, value));

            Column = null;
            Operator = null;
            Value = null;
        }

        protected void Cancel()
        {
            MudDialog.Cancel();
        }

        protected void Submit()
        {
            if (Column is not null && Operator is not null && !(_workingDefinitions?.Exists(d => d.Column == Column) ?? false))
            {
                CreateAndAdd(Column, Operator, Value);
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
