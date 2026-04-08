using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using QBittorrent.ApiClient;

namespace Lantean.QBTMud.Pages
{
    public partial class Tags
    {
        private const string _actionsColumnId = "actions";

        private readonly Dictionary<string, RenderFragment<RowContext<string>>> _columnRenderFragments = [];
        private IReadOnlyList<string>? _tags;
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

        [Inject]
        protected IApiFeedbackWorkflow ApiFeedbackWorkflow { get; set; } = default!;

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        [CascadingParameter]
        public MainData? MainData { get; set; }

        protected IEnumerable<string>? Results
        {
            get
            {
                if (_tags is not null)
                {
                    return _tags;
                }

                return MainData?.Tags;
            }
        }

        protected DynamicTable<string>? Table { get; set; }

        public Tags()
        {
            _columnRenderFragments.Add(_actionsColumnId, ActionsColumn);
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
                var tagsResult = await ApiClient.GetAllTagsAsync();
                if (!tagsResult.TryGetValue(out var tagList))
                {
                    await ApiFeedbackWorkflow.HandleFailureAsync(tagsResult);
                    return;
                }

                _tags = tagList;
            }
            finally
            {
                _isBusy = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        protected async Task DeleteTag(string? tag)
        {
            if (tag is null)
            {
                return;
            }
            var deleteResult = await ApiClient.DeleteTagsAsync(tags: [tag]);
            await ApiFeedbackWorkflow.HandleIfFailureAsync(deleteResult);
        }

        protected async Task AddTag()
        {
            var tag = await DialogWorkflow.ShowStringFieldDialog(
                LanguageLocalizer.Translate("TagFilterWidget", "New Tag"),
                LanguageLocalizer.Translate("TagFilterWidget", "Tag:"),
                null);

            if (tag is null)
            {
                return;
            }

            var existingTagsResult = await ApiClient.GetAllTagsAsync();
            if (!existingTagsResult.TryGetValue(out var existingTagList))
            {
                await ApiFeedbackWorkflow.HandleFailureAsync(existingTagsResult);
                return;
            }

            if (existingTagList.Contains(tag))
            {
                return;
            }

            var createResult = await ApiClient.CreateTagsAsync([tag]);
            await ApiFeedbackWorkflow.HandleIfFailureAsync(createResult);
        }

        protected IEnumerable<ColumnDefinition<string>> Columns => GetColumnDefinitions();

        private IEnumerable<ColumnDefinition<string>> GetColumnDefinitions()
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

        private List<ColumnDefinition<string>> ColumnsDefinitions => BuildColumnsDefinitions();

        private List<ColumnDefinition<string>> BuildColumnsDefinitions()
        {
            return
            [
                new ColumnDefinition<string>(LanguageLocalizer.Translate("TransferListModel", "Name"), l => l, id: "id"),
                new ColumnDefinition<string>(Translate("Actions"), l => l, id: _actionsColumnId)
            ];
        }

        private string Translate(string source, params object[] arguments)
        {
            return LanguageLocalizer.Translate("AppTags", source, arguments);
        }
    }
}
