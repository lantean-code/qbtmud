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
        private const string ActionsColumnId = "actions";

        private readonly Dictionary<string, RenderFragment<RowContext<Category>>> _columnRenderFragments = [];
        private IReadOnlyList<Category>? _categories;
        private bool _isBusy;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDialogWorkflow DialogWorkflow { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected ILocalStorageService LocalStorage { get; set; } = default!;

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        [CascadingParameter]
        public MainData? MainData { get; set; }

        protected IEnumerable<Category>? Results
        {
            get
            {
                if (_categories is not null)
                {
                    return _categories;
                }

                return MainData?.Categories.Values;
            }
        }

        protected DynamicTable<Category>? Table { get; set; }

        public Categories()
        {
            _columnRenderFragments.Add(ActionsColumnId, ActionsColumn);
        }

        protected void NavigateBack()
        {
            NavigationManager.NavigateToHome();
        }

        protected async Task Reload()
        {
            if (_isBusy)
            {
                return;
            }

            _isBusy = true;
            try
            {
                var categories = await ApiClient.GetAllCategories();
                _categories = categories.Values
                    .Select(category => new Category(category.Name, category.SavePath ?? string.Empty))
                    .ToList();
            }
            finally
            {
                _isBusy = false;
                await InvokeAsync(StateHasChanged);
            }
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
                if (_columnRenderFragments.TryGetValue(columnDefinition.Id, out var fragment))
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
                new ColumnDefinition<Category>(LanguageLocalizer.Translate("TransferListModel", "Name"), l => l.Name, id: "name"),
                new ColumnDefinition<Category>(LanguageLocalizer.Translate("TransferListModel", "Save path"), l => l.SavePath, id: "save_path"),
                new ColumnDefinition<Category>(Translate("Actions"), l => l, id: ActionsColumnId)
            ];
        }

        private string Translate(string source, params object[] arguments)
        {
            return LanguageLocalizer.Translate("AppCategories", source, arguments);
        }
    }
}
