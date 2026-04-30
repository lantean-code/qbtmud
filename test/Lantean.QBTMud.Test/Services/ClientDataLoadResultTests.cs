using System.Text.Json;
using AwesomeAssertions;
using Lantean.QBTMud.Services;
using QBittorrent.ApiClient;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class ClientDataLoadResultTests
    {
        [Fact]
        public void GIVEN_Entries_WHEN_FromEntries_THEN_ShouldCreateSuccessfulResultWithoutFailureResult()
        {
            var entries = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["QbtMud.Key"] = JsonDocument.Parse("\"Value\"").RootElement.Clone()
            };

            var result = ClientDataLoadResult.FromEntries(entries);

            result.Succeeded.Should().BeTrue();
            result.Entries.Should().BeSameAs(entries);
            result.FailureResult.Should().BeNull();
        }

        [Fact]
        public void GIVEN_FailedApiResult_WHEN_FromFailure_THEN_ShouldCreateFailedResultWithFailureResult()
        {
            var apiResult = ApiResult.CreateFailure(CreateFailure());

            var result = ClientDataLoadResult.FromFailure(apiResult);

            result.Succeeded.Should().BeFalse();
            result.Entries.Should().BeNull();
            result.FailureResult.Should().BeSameAs(apiResult);
        }

        [Fact]
        public void GIVEN_NullFailureResult_WHEN_FromFailure_THEN_ShouldThrowArgumentNullException()
        {
            Action action = () => ClientDataLoadResult.FromFailure(null!);

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GIVEN_SuccessfulApiResult_WHEN_FromFailure_THEN_ShouldThrowArgumentException()
        {
            Action action = () => ClientDataLoadResult.FromFailure(ApiResult.CreateSuccess());

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GIVEN_GenericFailure_WHEN_FailureRead_THEN_ShouldNotSetFailureResult()
        {
            var result = ClientDataLoadResult.Failure;

            result.Succeeded.Should().BeFalse();
            result.Entries.Should().BeNull();
            result.FailureResult.Should().BeNull();
        }

        private static ApiFailure CreateFailure()
        {
            return new ApiFailure
            {
                Detail = "Detail",
                IsTransient = true,
                Kind = ApiFailureKind.ServerError,
                Operation = "Operation",
                ResponseBody = "ResponseBody",
                UserMessage = "UserMessage"
            };
        }
    }
}
