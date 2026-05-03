using AwesomeAssertions;
using Lantean.QBTMud.Core.Helpers;
using Lantean.QBTMud.Core.Models;
using MudBlazor;

namespace Lantean.QBTMud.Application.Test.Services
{
    public sealed class TorrentQueryStateTests
    {
        [Fact]
        public void GIVEN_NewState_WHEN_ReadingProperties_THEN_DefaultValuesReturned()
        {
            var target = new TorrentQueryState();

            target.Category.Should().Be(FilterHelper.CATEGORY_ALL);
            target.Status.Should().Be(Status.All);
            target.Tag.Should().Be(FilterHelper.TAG_ALL);
            target.Tracker.Should().Be(FilterHelper.TRACKER_ALL);
            target.SearchText.Should().BeNull();
            target.SearchField.Should().Be(TorrentFilterField.Name);
            target.UseRegexSearch.Should().BeFalse();
            target.IsRegexValid.Should().BeTrue();
            target.SortColumn.Should().BeNull();
            target.SortDirection.Should().Be(SortDirection.None);
        }

        [Fact]
        public void GIVEN_NewCategory_WHEN_SetCategoryInvoked_THEN_StateUpdatesAndFilterChangeRaised()
        {
            var target = new TorrentQueryState();
            TorrentQueryStateChangedEventArgs? captured = null;
            target.Changed += (_, args) => captured = args;

            target.SetCategory("Category");

            target.Category.Should().Be("Category");
            captured.Should().NotBeNull();
            captured!.ChangeKind.Should().Be(TorrentQueryStateChangeKind.Filter);
        }

        [Fact]
        public void GIVEN_UnchangedCategory_WHEN_SetCategoryInvoked_THEN_DoesNotRaiseChange()
        {
            var target = new TorrentQueryState();
            var raised = false;
            target.Changed += (_, _) => raised = true;

            target.SetCategory(FilterHelper.CATEGORY_ALL);

            raised.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_NullCategory_WHEN_SetCategoryInvoked_THEN_ThrowsArgumentNullException()
        {
            var target = new TorrentQueryState();

            var action = () => target.SetCategory(null!);

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GIVEN_NewStatus_WHEN_SetStatusInvoked_THEN_StateUpdatesAndFilterChangeRaised()
        {
            var target = new TorrentQueryState();
            TorrentQueryStateChangedEventArgs? captured = null;
            target.Changed += (_, args) => captured = args;

            target.SetStatus(Status.Downloading);

            target.Status.Should().Be(Status.Downloading);
            captured.Should().NotBeNull();
            captured!.ChangeKind.Should().Be(TorrentQueryStateChangeKind.Filter);
        }

        [Fact]
        public void GIVEN_UnchangedStatus_WHEN_SetStatusInvoked_THEN_DoesNotRaiseChange()
        {
            var target = new TorrentQueryState();
            var raised = false;
            target.Changed += (_, _) => raised = true;

            target.SetStatus(Status.All);

            raised.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_NewTag_WHEN_SetTagInvoked_THEN_StateUpdatesAndFilterChangeRaised()
        {
            var target = new TorrentQueryState();
            TorrentQueryStateChangedEventArgs? captured = null;
            target.Changed += (_, args) => captured = args;

            target.SetTag("Tag");

            target.Tag.Should().Be("Tag");
            captured.Should().NotBeNull();
            captured!.ChangeKind.Should().Be(TorrentQueryStateChangeKind.Filter);
        }

        [Fact]
        public void GIVEN_UnchangedTag_WHEN_SetTagInvoked_THEN_DoesNotRaiseChange()
        {
            var target = new TorrentQueryState();
            var raised = false;
            target.Changed += (_, _) => raised = true;

            target.SetTag(FilterHelper.TAG_ALL);

            raised.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_NullTag_WHEN_SetTagInvoked_THEN_ThrowsArgumentNullException()
        {
            var target = new TorrentQueryState();

            var action = () => target.SetTag(null!);

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GIVEN_NewTracker_WHEN_SetTrackerInvoked_THEN_StateUpdatesAndFilterChangeRaised()
        {
            var target = new TorrentQueryState();
            TorrentQueryStateChangedEventArgs? captured = null;
            target.Changed += (_, args) => captured = args;

            target.SetTracker("Tracker");

            target.Tracker.Should().Be("Tracker");
            captured.Should().NotBeNull();
            captured!.ChangeKind.Should().Be(TorrentQueryStateChangeKind.Filter);
        }

        [Fact]
        public void GIVEN_UnchangedTracker_WHEN_SetTrackerInvoked_THEN_DoesNotRaiseChange()
        {
            var target = new TorrentQueryState();
            var raised = false;
            target.Changed += (_, _) => raised = true;

            target.SetTracker(FilterHelper.TRACKER_ALL);

            raised.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_NullTracker_WHEN_SetTrackerInvoked_THEN_ThrowsArgumentNullException()
        {
            var target = new TorrentQueryState();

            var action = () => target.SetTracker(null!);

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GIVEN_NewSearchState_WHEN_SetSearchInvoked_THEN_StateUpdatesAndFilterChangeRaised()
        {
            var target = new TorrentQueryState();
            TorrentQueryStateChangedEventArgs? captured = null;
            target.Changed += (_, args) => captured = args;
            var searchState = new FilterSearchState("Ubuntu", TorrentFilterField.SavePath, true, false);

            target.SetSearch(searchState);

            target.SearchText.Should().Be("Ubuntu");
            target.SearchField.Should().Be(TorrentFilterField.SavePath);
            target.UseRegexSearch.Should().BeTrue();
            target.IsRegexValid.Should().BeFalse();
            captured.Should().NotBeNull();
            captured!.ChangeKind.Should().Be(TorrentQueryStateChangeKind.Filter);
        }

        [Fact]
        public void GIVEN_UnchangedSearchState_WHEN_SetSearchInvoked_THEN_DoesNotRaiseChange()
        {
            var target = new TorrentQueryState();
            var raised = false;
            target.Changed += (_, _) => raised = true;

            target.SetSearch(new FilterSearchState(null, TorrentFilterField.Name, false, true));

            raised.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_NewSortColumn_WHEN_SetSortColumnInvoked_THEN_StateUpdatesAndSortChangeRaised()
        {
            var target = new TorrentQueryState();
            TorrentQueryStateChangedEventArgs? captured = null;
            target.Changed += (_, args) => captured = args;

            target.SetSortColumn("Name");

            target.SortColumn.Should().Be("Name");
            captured.Should().NotBeNull();
            captured!.ChangeKind.Should().Be(TorrentQueryStateChangeKind.Sort);
        }

        [Fact]
        public void GIVEN_UnchangedSortColumn_WHEN_SetSortColumnInvoked_THEN_DoesNotRaiseChange()
        {
            var target = new TorrentQueryState();
            var raised = false;
            target.Changed += (_, _) => raised = true;
            target.SetSortColumn("Name");
            raised = false;

            target.SetSortColumn("Name");

            raised.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_NewSortDirection_WHEN_SetSortDirectionInvoked_THEN_StateUpdatesAndSortChangeRaised()
        {
            var target = new TorrentQueryState();
            TorrentQueryStateChangedEventArgs? captured = null;
            target.Changed += (_, args) => captured = args;

            target.SetSortDirection(SortDirection.Descending);

            target.SortDirection.Should().Be(SortDirection.Descending);
            captured.Should().NotBeNull();
            captured!.ChangeKind.Should().Be(TorrentQueryStateChangeKind.Sort);
        }

        [Fact]
        public void GIVEN_UnchangedSortDirection_WHEN_SetSortDirectionInvoked_THEN_DoesNotRaiseChange()
        {
            var target = new TorrentQueryState();
            var raised = false;
            target.Changed += (_, _) => raised = true;

            target.SetSortDirection(SortDirection.None);

            raised.Should().BeFalse();
        }
    }
}
