using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Utilities;

namespace Lantean.QBTMud.Components.UI
{
    public partial class SortLabel : MudComponentBase
    {
        protected string Classname => new CssBuilder("mud-button-root mud-table-sort-label")
            .AddClass(Class)
            .Build();

        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        [Parameter]
        public SortDirection InitialDirection { get; set; } = SortDirection.None;

        /// <summary>
        /// Enable the sorting. Set to true by default.
        /// </summary>
        [Parameter]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Enable the sorting. Set to true by default.
        /// </summary>
        [Parameter]
        public bool AllowUnsorted { get; set; }

        /// <summary>
        /// The Icon used to display SortDirection.
        /// </summary>
        [Parameter]
        public string SortIcon { get; set; } = Icons.Material.Filled.ArrowUpward;

        /// <summary>
        /// If true the icon will be placed before the label text.
        /// </summary>
        [Parameter]
        public bool AppendIcon { get; set; }

        [Parameter]
        public SortDirection SortDirection { get; set; }

        [Parameter]
        public EventCallback<SortDirection> SortDirectionChanged { get; set; }

        public async Task ToggleSortDirection()
        {
            if (!Enabled)
            {
                return;
            }

            SortDirection sortDirection;
            switch (SortDirection)
            {
                case SortDirection.None:
                    sortDirection = SortDirection.Ascending;
                    break;

                case SortDirection.Ascending:
                    sortDirection = SortDirection.Descending;
                    break;

                case SortDirection.Descending:
                    sortDirection = AllowUnsorted ? SortDirection.None : SortDirection.Ascending;
                    break;

                default:
                    sortDirection = SortDirection.None;
                    break;
            }

            await SortDirectionChanged.InvokeAsync(sortDirection);
        }

        private string GetSortIconClass()
        {
            if (SortDirection == SortDirection.Descending)
            {
                return "mud-table-sort-label-icon mud-direction-desc";
            }

            if (SortDirection == SortDirection.Ascending)
            {
                return "mud-table-sort-label-icon mud-direction-asc";
            }

            return "mud-table-sort-label-icon";
        }
    }
}