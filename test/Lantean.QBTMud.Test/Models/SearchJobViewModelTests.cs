using AwesomeAssertions;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Test.Models
{
    public sealed class SearchJobViewModelTests
    {
        [Fact]
        public void GIVEN_SearchJob_WHEN_UpdateStatusToCompleted_THEN_CompletedOnSet()
        {
            var job = new SearchJobViewModel(1, "Ubuntu", new[] { "movies" }, SearchForm.AllCategoryId);

            job.AppendResults(new[]
            {
                new SearchResult("http://desc", "Ubuntu", 1_000_000, "http://files", 1, 10, "http://site", "movies", 1_700_000_000)
            });

            job.UpdateStatus("Completed", 1);

            job.Status.Should().Be("Completed");
            job.CompletedOn.Should().NotBeNull();
            job.CurrentOffset.Should().Be(1);

            job.SetError("failed");
            job.Status.Should().Be("Error");
            job.ErrorMessage.Should().Be("failed");

            job.ResetResults(clearError: false);
            job.Results.Should().BeEmpty();
            job.ErrorMessage.Should().Be("failed");

            job.ResetResults();
            job.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public void GIVEN_SearchJob_WHEN_MatchesCalled_THEN_ComparesAllCriteria()
        {
            var job = new SearchJobViewModel(2, "Ubuntu", new[] { "movies", "tv" }, SearchForm.AllCategoryId);

            job.Matches("Ubuntu", SearchForm.AllCategoryId, new[] { "movies", "tv" }).Should().BeTrue();
            job.Matches("Ubuntu", SearchForm.AllCategoryId, new[] { "movies" }).Should().BeFalse();
            job.Matches("Fedora", SearchForm.AllCategoryId, new[] { "movies", "tv" }).Should().BeFalse();
            job.Matches("Ubuntu", "movies", new[] { "movies", "tv" }).Should().BeFalse();
        }
    }
}
