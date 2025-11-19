using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Pages;
using MudBlazor;

namespace Lantean.QBTMud.Test.Infrastructure
{
    internal sealed class SearchTestHost : Search
    {
        public Task InvokeHandleResultContextMenu(TableDataContextMenuEventArgs<SearchResult> args)
        {
            return HandleResultContextMenu(args);
        }

        public Task InvokeHandleResultLongPress(TableDataLongPressEventArgs<SearchResult> args)
        {
            return HandleResultLongPress(args);
        }

        public Task InvokeDownloadResultFromContext()
        {
            return DownloadResultFromContext();
        }

        public Task InvokeCopyNameFromContext()
        {
            return CopyNameFromContext();
        }

        public Task InvokeCopyDownloadLinkFromContext()
        {
            return CopyDownloadLinkFromContext();
        }

        public Task InvokeCopyDescriptionLinkFromContext()
        {
            return CopyDescriptionLinkFromContext();
        }

        public Task InvokeOpenDescriptionFromContext()
        {
            return OpenDescriptionFromContext();
        }

        public bool HasContextResultValue => HasContextResult;

        public bool ShowAdvancedFiltersValue => ShowAdvancedFilters;

        public bool ShowSearchFormValue => ShowSearchForm;

        public void SetBreakpoint(Breakpoint breakpoint)
        {
            CurrentBreakpoint = breakpoint;
        }

        public IReadOnlyList<SearchJobViewModel> ExposedJobs => Jobs;

        public Task InvokeStopJob(SearchJobViewModel job)
        {
            return StopJob(job);
        }

        public Task InvokeRefreshJob(SearchJobViewModel job)
        {
            return RefreshJob(job);
        }

        public Task InvokeCloseAllJobs()
        {
            return CloseAllJobs();
        }

        public void ClearResultContextMenuReference()
        {
            ResultContextMenu = null;
        }
    }
}
