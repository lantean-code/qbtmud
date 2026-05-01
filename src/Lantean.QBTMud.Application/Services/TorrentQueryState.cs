using Lantean.QBTMud.Core.Helpers;
using Lantean.QBTMud.Core.Models;
using MudBlazor;

namespace Lantean.QBTMud.Application.Services
{
    /// <summary>
    /// Stores the current torrent query state shared across the authenticated shell.
    /// </summary>
    public sealed class TorrentQueryState : ITorrentQueryState
    {
        private string _category = FilterHelper.CATEGORY_ALL;
        private Status _status = Status.All;
        private string _tag = FilterHelper.TAG_ALL;
        private string _tracker = FilterHelper.TRACKER_ALL;
        private string? _searchText;
        private TorrentFilterField _searchField = TorrentFilterField.Name;
        private bool _useRegexSearch;
        private bool _isRegexValid = true;
        private string? _sortColumn;
        private SortDirection _sortDirection;

        /// <inheritdoc />
        public event EventHandler<TorrentQueryStateChangedEventArgs>? Changed;

        /// <inheritdoc />
        public string Category
        {
            get { return _category; }
        }

        /// <inheritdoc />
        public Status Status
        {
            get { return _status; }
        }

        /// <inheritdoc />
        public string Tag
        {
            get { return _tag; }
        }

        /// <inheritdoc />
        public string Tracker
        {
            get { return _tracker; }
        }

        /// <inheritdoc />
        public string? SearchText
        {
            get { return _searchText; }
        }

        /// <inheritdoc />
        public TorrentFilterField SearchField
        {
            get { return _searchField; }
        }

        /// <inheritdoc />
        public bool UseRegexSearch
        {
            get { return _useRegexSearch; }
        }

        /// <inheritdoc />
        public bool IsRegexValid
        {
            get { return _isRegexValid; }
        }

        /// <inheritdoc />
        public string? SortColumn
        {
            get { return _sortColumn; }
        }

        /// <inheritdoc />
        public SortDirection SortDirection
        {
            get { return _sortDirection; }
        }

        /// <inheritdoc />
        public void SetCategory(string category)
        {
            ArgumentNullException.ThrowIfNull(category);

            if (string.Equals(_category, category, StringComparison.Ordinal))
            {
                return;
            }

            _category = category;
            RaiseChanged(TorrentQueryStateChangeKind.Filter);
        }

        /// <inheritdoc />
        public void SetStatus(Status status)
        {
            if (_status == status)
            {
                return;
            }

            _status = status;
            RaiseChanged(TorrentQueryStateChangeKind.Filter);
        }

        /// <inheritdoc />
        public void SetTag(string tag)
        {
            ArgumentNullException.ThrowIfNull(tag);

            if (string.Equals(_tag, tag, StringComparison.Ordinal))
            {
                return;
            }

            _tag = tag;
            RaiseChanged(TorrentQueryStateChangeKind.Filter);
        }

        /// <inheritdoc />
        public void SetTracker(string tracker)
        {
            ArgumentNullException.ThrowIfNull(tracker);

            if (string.Equals(_tracker, tracker, StringComparison.Ordinal))
            {
                return;
            }

            _tracker = tracker;
            RaiseChanged(TorrentQueryStateChangeKind.Filter);
        }

        /// <inheritdoc />
        public void SetSearch(FilterSearchState filterSearchState)
        {
            if (string.Equals(_searchText, filterSearchState.Text, StringComparison.Ordinal)
                && _searchField == filterSearchState.Field
                && _useRegexSearch == filterSearchState.UseRegex
                && _isRegexValid == filterSearchState.IsRegexValid)
            {
                return;
            }

            _searchText = filterSearchState.Text;
            _searchField = filterSearchState.Field;
            _useRegexSearch = filterSearchState.UseRegex;
            _isRegexValid = filterSearchState.IsRegexValid;
            RaiseChanged(TorrentQueryStateChangeKind.Filter);
        }

        /// <inheritdoc />
        public void SetSortColumn(string? sortColumn)
        {
            if (string.Equals(_sortColumn, sortColumn, StringComparison.Ordinal))
            {
                return;
            }

            _sortColumn = sortColumn;
            RaiseChanged(TorrentQueryStateChangeKind.Sort);
        }

        /// <inheritdoc />
        public void SetSortDirection(SortDirection sortDirection)
        {
            if (_sortDirection == sortDirection)
            {
                return;
            }

            _sortDirection = sortDirection;
            RaiseChanged(TorrentQueryStateChangeKind.Sort);
        }

        private void RaiseChanged(TorrentQueryStateChangeKind changeKind)
        {
            Changed?.Invoke(this, new TorrentQueryStateChangedEventArgs(changeKind));
        }
    }
}
