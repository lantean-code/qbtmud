using Blazored.LocalStorage;
using Lantean.QBitTorrentClient;
using Lantean.QBTMudBlade.Components.UI;
using Lantean.QBTMudBlade.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMudBlade.Pages
{
    public partial class TagManagement
    {
        private readonly Dictionary<string, RenderFragment<RowContext<string>>> _columnRenderFragments = [];

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDialogService DialogService { get; set; } = default!;

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

        public TagManagement()
        {
            _columnRenderFragments.Add("Actions", ActionsColumn);
        }

        protected void NavigateBack()
        {
            NavigationManager.NavigateTo("/");
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
            var tags = await DialogService.ShowAddTagsDialog();

            if (tags is null || tags.Count == 0)
            {
                return;
            }

            await ApiClient.CreateTags(tags);
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