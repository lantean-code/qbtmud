using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMud.Pages
{
    public partial class Categories
    {
        private readonly Dictionary<string, RenderFragment<RowContext<Category>>> _columnRenderFragments = [];

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDialogWorkflow DialogWorkflow { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected ILocalStorageService LocalStorage { get; set; } = default!;

        [Inject]
        protected IWebUiLocalizer WebUiLocalizer { get; set; } = default!;

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        [CascadingParameter]
        public MainData? MainData { get; set; }

        protected IEnumerable<Category>? Results => MainData?.Categories.Values;

        protected DynamicTable<Category>? Table { get; set; }

        public Categories()
        {
            _columnRenderFragments.Add("Actions", ActionsColumn);
        }

        protected void NavigateBack()
        {
            NavigationManager.NavigateToHome();
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
            await DialogWorkflow.InvokeAddCategoryDialog();
        }

        protected async Task EditCategory(string? name)
        {
            if (name is null)
            {
                return;
            }
            await DialogWorkflow.InvokeEditCategoryDialog(name);
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

        private List<ColumnDefinition<Category>> ColumnsDefinitions => BuildColumnsDefinitions();

        private List<ColumnDefinition<Category>> BuildColumnsDefinitions()
        {
            return
            [
                new ColumnDefinition<Category>(WebUiLocalizer.Translate("TransferListModel", "Name"), l => l.Name),
                new ColumnDefinition<Category>(WebUiLocalizer.Translate("TransferListModel", "Save path"), l => l.SavePath),
                new ColumnDefinition<Category>("Actions", l => l)
            ];
        }
    }
}
