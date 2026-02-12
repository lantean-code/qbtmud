using Moq;

namespace Lantean.QBTMud.Test.Infrastructure
{
    internal static class MockInvocationExtensions
    {
        public static void ClearInvocations(this Mock mock)
        {
            var invocations = mock.Invocations;
            invocations.Clear();
        }

        public static void ClearInvocations<T>(this T value)
            where T : class
        {
            if (value is Mock mock)
            {
                mock.ClearInvocations();
                return;
            }

            var invocations = Mock.Get(value).Invocations;
            invocations.Clear();
        }
    }
}
