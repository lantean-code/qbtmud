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

        private static string GetRepositoryRoot([CallerFilePath] string sourceFilePath = "")
        {
            return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFilePath)!, "..", "..", ".."));
        }
    }
}
