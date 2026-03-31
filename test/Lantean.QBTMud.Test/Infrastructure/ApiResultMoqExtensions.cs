using Moq.Language;
using Moq.Language.Flow;
using QBittorrent.ApiClient;
using System.Net;

namespace Moq
{
    internal static class ApiResultMoqExtensions
    {
        internal static IReturnsResult<TMock> Returns<TMock>(this IReturns<TMock, Task<ApiResult>> setup, Task task)
            where TMock : class
        {
            return setup.Returns(async () =>
            {
                await task;
                return ApiResult.Success();
            });
        }

        internal static IReturnsResult<TMock> Returns<TMock>(this IReturns<TMock, Task<ApiResult>> setup, Func<Task> taskFactory)
            where TMock : class
        {
            return setup.Returns(async () =>
            {
                await taskFactory();
                return ApiResult.Success();
            });
        }

        internal static IReturnsResult<TMock> Returns<TMock, TValue>(this IReturns<TMock, Task<ApiResult<TValue>>> setup, Task<TValue> task)
            where TMock : class
        {
            return setup.Returns(async () =>
            {
                var value = await task;
                return ApiResult<TValue>.Success(value);
            });
        }

        internal static IReturnsResult<TMock> Returns<TMock, TValue>(this IReturns<TMock, Task<ApiResult<TValue>>> setup, Func<Task<TValue>> taskFactory)
            where TMock : class
        {
            return setup.Returns(async () =>
            {
                var value = await taskFactory();
                return ApiResult<TValue>.Success(value);
            });
        }

        internal static IReturnsResult<TMock> ReturnsAsync<TMock, TValue>(this IReturns<TMock, Task<ApiResult<TValue>>> setup, TValue value)
            where TMock : class
        {
            return setup.ReturnsAsync(ApiResult<TValue>.Success(value));
        }

        internal static IReturnsResult<TMock> ReturnsFailure<TMock>(this IReturns<TMock, Task<ApiResult>> setup, ApiFailureKind kind, string userMessage, HttpStatusCode? statusCode = null, object? reason = null)
            where TMock : class
        {
            return setup.ReturnsAsync(ApiResult.FailureResult(CreateFailure(kind, userMessage, statusCode, reason)));
        }

        internal static IReturnsResult<TMock> ReturnsFailure<TMock, TValue>(this IReturns<TMock, Task<ApiResult<TValue>>> setup, ApiFailureKind kind, string userMessage, HttpStatusCode? statusCode = null, object? reason = null)
            where TMock : class
        {
            return setup.ReturnsAsync(ApiResult<TValue>.FailureResult(CreateFailure(kind, userMessage, statusCode, reason)));
        }

        internal static IReturnsResult<TMock> ReturnsAsync<TMock, TValue>(this IReturns<TMock, Task<ApiResult<TValue>>> setup, Func<TValue> valueFunction)
            where TMock : class
        {
            return setup.ReturnsAsync(() => ApiResult<TValue>.Success(valueFunction()));
        }

        internal static IReturnsResult<TMock> ReturnsAsync<TMock, T1, TValue>(this IReturns<TMock, Task<ApiResult<TValue>>> setup, Func<T1, TValue> valueFunction)
            where TMock : class
        {
            return setup.ReturnsAsync((T1 arg1) => ApiResult<TValue>.Success(valueFunction(arg1)));
        }

        internal static IReturnsResult<TMock> ReturnsAsync<TMock, T1, T2, TValue>(this IReturns<TMock, Task<ApiResult<TValue>>> setup, Func<T1, T2, TValue> valueFunction)
            where TMock : class
        {
            return setup.ReturnsAsync((T1 arg1, T2 arg2) => ApiResult<TValue>.Success(valueFunction(arg1, arg2)));
        }

        internal static IReturnsResult<TMock> ReturnsAsync<TMock, T1, T2, T3, TValue>(this IReturns<TMock, Task<ApiResult<TValue>>> setup, Func<T1, T2, T3, TValue> valueFunction)
            where TMock : class
        {
            return setup.ReturnsAsync((T1 arg1, T2 arg2, T3 arg3) => ApiResult<TValue>.Success(valueFunction(arg1, arg2, arg3)));
        }

        internal static ISetupSequentialResult<Task<ApiResult>> Returns(this ISetupSequentialResult<Task<ApiResult>> setup, Task task)
        {
            return setup.Returns(async () =>
            {
                await task;
                return ApiResult.Success();
            });
        }

        internal static ISetupSequentialResult<Task<ApiResult<TValue>>> Returns<TValue>(this ISetupSequentialResult<Task<ApiResult<TValue>>> setup, Task<TValue> task)
        {
            return setup.Returns(async () =>
            {
                var value = await task;
                return ApiResult<TValue>.Success(value);
            });
        }

        internal static ISetupSequentialResult<Task<ApiResult<TValue>>> ReturnsAsync<TValue>(this ISetupSequentialResult<Task<ApiResult<TValue>>> setup, TValue value)
        {
            return setup.ReturnsAsync(ApiResult<TValue>.Success(value));
        }

        internal static ISetupSequentialResult<Task<ApiResult>> ReturnsFailure(this ISetupSequentialResult<Task<ApiResult>> setup, ApiFailureKind kind, string userMessage, HttpStatusCode? statusCode = null, object? reason = null)
        {
            return setup.ReturnsAsync(ApiResult.FailureResult(CreateFailure(kind, userMessage, statusCode, reason)));
        }

        internal static ISetupSequentialResult<Task<ApiResult<TValue>>> ReturnsFailure<TValue>(this ISetupSequentialResult<Task<ApiResult<TValue>>> setup, ApiFailureKind kind, string userMessage, HttpStatusCode? statusCode = null, object? reason = null)
        {
            return setup.ReturnsAsync(ApiResult<TValue>.FailureResult(CreateFailure(kind, userMessage, statusCode, reason)));
        }

        private static ApiFailure CreateFailure(ApiFailureKind kind, string userMessage, HttpStatusCode? statusCode, object? reason)
        {
            return new ApiFailure
            {
                Kind = kind,
                Operation = "test",
                StatusCode = statusCode,
                UserMessage = userMessage,
                Detail = userMessage,
                Reason = reason,
                ResponseBody = userMessage,
                IsTransient = kind is ApiFailureKind.NoResponse or ApiFailureKind.Timeout or ApiFailureKind.ServerError,
            };
        }
    }
}
