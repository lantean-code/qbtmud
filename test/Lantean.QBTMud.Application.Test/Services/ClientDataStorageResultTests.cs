using AwesomeAssertions;
using QBittorrent.ApiClient;

namespace Lantean.QBTMud.Application.Test.Services
{
    public sealed class ClientDataStorageResultTests
    {
        [Fact]
        public void GIVEN_SuccessResult_WHEN_SuccessRead_THEN_ShouldNotSetFailureResult()
        {
            var result = ClientDataStorageResult.Success;

            result.Succeeded.Should().BeTrue();
            result.FailureResult.Should().BeNull();
        }

        [Fact]
        public void GIVEN_FailedApiResult_WHEN_FromFailure_THEN_ShouldCreateFailedResultWithFailureResult()
        {
            var apiResult = ApiResult.CreateFailure(CreateFailure());

            var result = ClientDataStorageResult.FromFailure(apiResult);

            result.Succeeded.Should().BeFalse();
            result.FailureResult.Should().BeSameAs(apiResult);
        }

        [Fact]
        public void GIVEN_NullFailureResult_WHEN_FromFailure_THEN_ShouldThrowArgumentNullException()
        {
            Action action = () => ClientDataStorageResult.FromFailure(null!);

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GIVEN_SuccessfulApiResult_WHEN_FromFailure_THEN_ShouldThrowArgumentException()
        {
            Action action = () => ClientDataStorageResult.FromFailure(ApiResult.CreateSuccess());

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GIVEN_GenericFailure_WHEN_FailureRead_THEN_ShouldNotSetFailureResult()
        {
            var result = ClientDataStorageResult.Failure;

            result.Succeeded.Should().BeFalse();
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
