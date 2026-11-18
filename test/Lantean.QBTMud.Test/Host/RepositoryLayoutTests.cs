using System.Runtime.CompilerServices;
using System.Xml.Linq;
using AwesomeAssertions;

namespace Lantean.QBTMud.Test.Host
{
    public sealed class RepositoryLayoutTests
    {
        [Fact]
        public void GIVEN_SolutionProjects_WHEN_RepositoryInventoryRead_THEN_ShouldListExactlyCurrentProjects()
        {
            var repositoryRoot = GetRepositoryRoot();
            var solution = XDocument.Load(Path.Combine(repositoryRoot, "Lantean.QBTMud.slnx"));
            var expectedProjects = solution.Descendants("Project").Select(project => project.Attribute("Path")!.Value).ToList();
            var instructions = File.ReadAllText(Path.Combine(repositoryRoot, "AGENTS.md"));
            var inventory = instructions.Split("## Repository layout", StringSplitOptions.None)[1].Split("## Build, test, publish", StringSplitOptions.None)[0];
            var documentedProjects = inventory.Split('`').Where(value => value.EndsWith(".csproj", StringComparison.Ordinal)).ToList();

            documentedProjects.Should().BeEquivalentTo(expectedProjects);
            documentedProjects.Should().OnlyContain(path => File.Exists(Path.Combine(repositoryRoot, path)));
        }

        [Fact]
        public void GIVEN_JavaScriptTestInstructions_WHEN_ReadingTestCommand_THEN_ShouldConfigureArtifactsWithoutUnsupportedRunnerArguments()
        {
            var repositoryRoot = GetRepositoryRoot();
            var instructions = File.ReadAllText(Path.Combine(repositoryRoot, "test", "Lantean.QBTMud.Test", "JavaScript", "README.md"));
            var command = instructions.Split('\n').Single(line => line.StartsWith("dotnet test ", StringComparison.Ordinal));

            instructions.Should().Contain("$env:ArtifactsPath='/tmp/artifacts/qbtmud'");
            instructions.Should().Contain("$env:UseArtifactsOutput='true'");
            command.Should().Contain("--project test/Lantean.QBTMud.Test/Lantean.QBTMud.Test.csproj");
            command.Should().Contain("--filter-namespace Lantean.QBTMud.Test.JavaScript");
            command.Should().NotContain("--artifacts-path");
            command.Should().NotContain("/p:");
        }

        [Fact]
        public void GIVEN_CoverageWorkflow_WHEN_ReadingTestStep_THEN_ShouldConfigureBuildPropertiesWithoutUnsupportedRunnerArguments()
        {
            var repositoryRoot = GetRepositoryRoot();
            var workflow = File.ReadAllText(Path.Combine(repositoryRoot, ".github", "workflows", "dotnet.yml"));
            var testStep = workflow.Split("- name: Run Tests with Coverage", StringSplitOptions.None)[1].Split("- name:", StringSplitOptions.None)[0];
            var command = testStep.Split('\n').Single(line => line.TrimStart().StartsWith("run: dotnet test ", StringComparison.Ordinal));

            testStep.Should().Contain("ArtifactsPath: /tmp/artifacts/qbtmud");
            testStep.Should().Contain("UseArtifactsOutput: 'true'");
            testStep.Should().Contain("QbtMudBuildVersion: ${{ env.VERSION }}");
            testStep.Should().Contain("QBTMUD_JS_COVERAGE_DIR:");
            command.Should().Contain("--no-build --configuration Release --coverlet");
            command.Should().NotContain("--artifacts-path");
            command.Should().NotContain("/p:");
        }

        private static string GetRepositoryRoot([CallerFilePath] string sourceFilePath = "")
        {
            return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFilePath)!, "..", "..", ".."));
        }
    }
}
