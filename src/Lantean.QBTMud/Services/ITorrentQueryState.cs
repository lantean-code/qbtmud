using Lantean.QBTMud.Models;
using MudBlazor;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Provides the current torrent query state shared across the authenticated shell.
    /// </summary>
    public interface ITorrentQueryState
    {
        /// <summary>
        /// Occurs when the torrent query state changes.
        /// </summary>
        event EventHandler<TorrentQueryStateChangedEventArgs>? Changed;

        /// <summary>
        /// Gets the selected category filter.
        /// </summary>
        string Category { get; }

        /// <summary>
        /// Gets the selected status filter.
        /// </summary>
        Status Status { get; }

        /// <summary>
        /// Gets the selected tag filter.
        /// </summary>
        string Tag { get; }

        /// <summary>
        /// Gets the selected tracker filter.
        /// </summary>
        string Tracker { get; }

        /// <summary>
        /// Gets the search text.
        /// </summary>
        string? SearchText { get; }

        /// <summary>
        /// Gets the selected search field.
        /// </summary>
        TorrentFilterField SearchField { get; }

        /// <summary>
        /// Gets a value indicating whether regex search is enabled.
        /// </summary>
        bool UseRegexSearch { get; }

        /// <summary>
        /// Gets a value indicating whether the current regex state is valid.
        /// </summary>
        bool IsRegexValid { get; }

        /// <summary>
        /// Gets the selected sort column.
        /// </summary>
        string? SortColumn { get; }

        /// <summary>
        /// Gets the selected sort direction.
        /// </summary>
        SortDirection SortDirection { get; }

        /// <summary>
        /// Sets the selected category filter.
        /// </summary>
        /// <param name="category">The category value.</param>
        void SetCategory(string category);

        /// <summary>
        /// Sets the selected status filter.
        /// </summary>
        /// <param name="status">The status value.</param>
        void SetStatus(Status status);

        /// <summary>
        /// Sets the selected tag filter.
        /// </summary>
        /// <param name="tag">The tag value.</param>
        void SetTag(string tag);

        /// <summary>
        /// Sets the selected tracker filter.
        /// </summary>
        /// <param name="tracker">The tracker value.</param>
        void SetTracker(string tracker);

        /// <summary>
        /// Sets the search state.
        /// </summary>
        /// <param name="filterSearchState">The search state.</param>
        void SetSearch(FilterSearchState filterSearchState);

        /// <summary>
        /// Sets the selected sort column.
        /// </summary>
        /// <param name="sortColumn">The sort column identifier.</param>
        void SetSortColumn(string? sortColumn);

        /// <summary>
        /// Sets the selected sort direction.
        /// </summary>
        /// <param name="sortDirection">The sort direction.</param>
        void SetSortDirection(SortDirection sortDirection);
    }
}
