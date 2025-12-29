using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Pages
{
    public partial class Tags
    {
        private readonly Dictionary<string, RenderFragment<RowContext<string>>> _columnRenderFragments = [];

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDialogWorkflow DialogWorkflow { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected ILocalStorageService LocalStorage { get; set; } = default!;

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        [CascadingParameter]
        public MainData? MainData { get; set; }

        protected IEnumerable<string>? Results => MainData?.Tags;

        protected DynamicTable<string>? Table { get; set; }

        public Tags()
        {
            _columnRenderFragments.Add("Actions", ActionsColumn);
        }

        protected void NavigateBack()
        {
            NavigationManager.NavigateToHome();
        }

        protected async Task DeleteTag(string? tag)
        {
            if (tag is null)
            {
                return;
            }
            await ApiClient.DeleteTags(tag);
        }

        protected async Task AddTag()
        {
            var tag = await DialogWorkflow.ShowStringFieldDialog("Add Tag", "Tag", null);

            if (tag is null)
            {
                return;
            }

            var existingTags = await ApiClient.GetAllTags();
            if (existingTags.Contains(tag))
            {
                return;
            }

            await ApiClient.CreateTags([tag]);
        }

        protected IEnumerable<ColumnDefinition<string>> Columns => GetColumnDefinitions();

        private IEnumerable<ColumnDefinition<string>> GetColumnDefinitions()
        {
            foreach (var columnDefinition in ColumnsDefinitions)
            {
                if (_columnRenderFragments.TryGetValue(columnDefinition.Header, out var fragment))
                {
                    columnDefinition.RowTemplate = fragment;
                }

                yield return columnDefinition;
            }
        }

        public static List<ColumnDefinition<string>> ColumnsDefinitions { get; } =
        [
            new ColumnDefinition<string>("Id", l => l),
            new ColumnDefinition<string>("Actions", l => l)
        ];
    }
}
