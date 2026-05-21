using AwesomeAssertions;
using QbtMudTranslations;

namespace QbtMudTranslations.Test
{
    public sealed class TranslationRetryOptionsTests : IDisposable
    {
        public TranslationRetryOptionsTests()
        {
            Environment.SetEnvironmentVariable("QBTMUD_TRANSLATIONS_MAX_VALIDATION_RETRIES", null);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("QBTMUD_TRANSLATIONS_MAX_VALIDATION_RETRIES", null);
        }

        [Fact]
        public void GIVEN_MaxValidationRetriesEnvironmentVariableIsMissing_WHEN_FromEnvironment_THEN_ShouldUseDefaultRetryLimit()
        {
            var result = TranslationRetryOptions.FromEnvironment();

            result.MaxValidationRetries.Should().Be(1);
        }

        [Fact]
        public void GIVEN_MaxValidationRetriesEnvironmentVariableIsSet_WHEN_FromEnvironment_THEN_ShouldUseConfiguredRetryLimit()
        {
            Environment.SetEnvironmentVariable("QBTMUD_TRANSLATIONS_MAX_VALIDATION_RETRIES", "0");

            var result = TranslationRetryOptions.FromEnvironment();

            result.MaxValidationRetries.Should().Be(0);
        }

        [Fact]
        public void GIVEN_MaxValidationRetriesEnvironmentVariableIsOutOfRange_WHEN_FromEnvironment_THEN_ShouldThrow()
        {
            Environment.SetEnvironmentVariable("QBTMUD_TRANSLATIONS_MAX_VALIDATION_RETRIES", "4");

            var action = TranslationRetryOptions.FromEnvironment;

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("QBTMUD_TRANSLATIONS_MAX_VALIDATION_RETRIES must be an integer between 0 and 3.");
        }
    }
}
