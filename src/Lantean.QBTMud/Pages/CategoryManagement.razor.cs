using Blazored.LocalStorage;
using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Pages
{
    public partial class CategoryManagement
    {
        private readonly Dictionary<string, RenderFragment<RowContext<Category>>> _columnRenderFragments = [];

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

        protected IEnumerable<Category>? Results => MainData?.Categories.Values;

        protected DynamicTable<Category>? Table { get; set; }

        public CategoryManagement()
        {
            _columnRenderFragments.Add("Actions", ActionsColumn);
        }

        protected void NavigateBack()
        {
            NavigationManager.NavigateTo("/");
        }

        protected async Task DeleteCategory(string? name)
        {
            if (name is null)
            {
                return;
            }
            await ApiClient.RemoveCategories(name);
        }

        protected async Task AddCategory()
        {
            await DialogService.InvokeAddCategoryDialog(ApiClient);
        }

        protected async Task EditCategory(string? name)
        {
            if (name is null)
            {
                return;
            }
            await DialogService.InvokeEditCategoryDialog(ApiClient, name);
        }

        protected IEnumerable<ColumnDefinition<Category>> Columns => GetColumnDefinitions();

        private IEnumerable<ColumnDefinition<Category>> GetColumnDefinitions()
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

        public static List<ColumnDefinition<Category>> ColumnsDefinitions { get; } =
        [
            new ColumnDefinition<Category>("Name", l => l.Name),
            new ColumnDefinition<Category>("Save path", l => l.SavePath),
            new ColumnDefinition<Category>("Actions", l => l)
        ];
    }
}