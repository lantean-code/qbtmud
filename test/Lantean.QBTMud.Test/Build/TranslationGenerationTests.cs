using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;
using AwesomeAssertions;

namespace Lantean.QBTMud.Test.Build
{
    public sealed class TranslationGenerationTests
    {
        [Fact]
        public async Task GIVEN_CheckoutPathContainingSpaces_WHEN_ActualTranslationTargetRuns_THEN_ShouldGenerateTranslationsWithoutChangingOriginalInputs()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var repositoryRoot = FindRepositoryRoot();
            var appProjectPath = Path.Combine(repositoryRoot, "src", "Lantean.QBTMud", "Lantean.QBTMud.csproj");
            var converterProjectPath = Path.Combine(repositoryRoot, "tools", "WebUiTranslationsConverter", "WebUiTranslationsConverter.csproj");
            var converterSourcePath = Path.Combine(repositoryRoot, "tools", "WebUiTranslationsConverter", "Program.cs");
            var sdkPath = Path.Combine(repositoryRoot, "global.json");
            var originalPaths = new[] { appProjectPath, converterProjectPath, converterSourcePath, sdkPath };
            var originalHashes = GetInputHashes(originalPaths);
            var temporaryDirectory = Directory.CreateTempSubdirectory("qbtmud translation generation ");
            var scratchRoot = temporaryDirectory.FullName;
            Console.WriteLine($"Translation generation scratch directory: {scratchRoot}");

            try
            {
                var appDirectory = Path.Combine(scratchRoot, "src", "Lantean.QBTMud");
                var converterDirectory = Path.Combine(scratchRoot, "tools", "WebUiTranslationsConverter");
                var translationsDirectory = Path.Combine(scratchRoot, "external", "qBittorrent", "src", "webui", "www", "translations");
                Directory.CreateDirectory(appDirectory);
                Directory.CreateDirectory(converterDirectory);
                Directory.CreateDirectory(translationsDirectory);
                File.Copy(converterProjectPath, Path.Combine(converterDirectory, "WebUiTranslationsConverter.csproj"));
                File.Copy(converterSourcePath, Path.Combine(converterDirectory, "Program.cs"));
                File.Copy(sdkPath, Path.Combine(scratchRoot, "global.json"));
                await File.WriteAllTextAsync(Path.Combine(translationsDirectory, "webui_en.ts"),
                    "<TS language=\"en\"><context><name>Context</name><message><source>Source</source><translation>Translation</translation></message></context></TS>", cancellationToken);
                await File.WriteAllTextAsync(Path.Combine(scratchRoot, "NuGet.Config"),
                    "<configuration><packageSources><clear /></packageSources></configuration>", cancellationToken);

                var artifactSuffix = Guid.NewGuid().ToString("N");
                var artifactProperties = new XDocument(new XElement("Project", new XElement("PropertyGroup",
                    new XElement("ArtifactsPath", "/tmp/artifacts/qbtmud"),
                    new XElement("UseArtifactsOutput", "true"),
                    new XElement("ArtifactsProjectName", $"$(MSBuildProjectName)-TranslationGeneration-{artifactSuffix}"),
                    new XElement("NuGetAudit", "false"))));
                artifactProperties.Save(Path.Combine(scratchRoot, "Directory.Build.props"));
                var sourceProject = XDocument.Load(appProjectPath).Root!;
                var translationProperties = sourceProject.Elements("PropertyGroup")
                    .Single(group => group.Element("WebUiTranslationsSource") is not null);
                var framework = sourceProject.Elements("PropertyGroup").Elements("TargetFramework").Single().Value;
                var scratchProject = new XDocument(new XElement("Project", new XAttribute("Sdk", "Microsoft.NET.Sdk"),
                    new XElement("PropertyGroup", new XElement("TargetFramework", framework), new XElement("EnableDefaultCompileItems", "false")),
                    new XElement(translationProperties),
                    new XElement(sourceProject.Elements("Target").Single(target => (string?)target.Attribute("Name") == "ResolveQbtMudTranslationFiles")),
                    new XElement(sourceProject.Elements("Target").Single(target => (string?)target.Attribute("Name") == "GenerateWebUiTranslations"))));
                var scratchProjectPath = Path.Combine(appDirectory, "Lantean.QBTMud.csproj");
                scratchProject.Save(scratchProjectPath);

                var startInfo = new ProcessStartInfo("dotnet")
                {
                    WorkingDirectory = appDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                startInfo.ArgumentList.Add("build");
                startInfo.ArgumentList.Add(scratchProjectPath);
                startInfo.ArgumentList.Add("--target:GenerateWebUiTranslations");
                startInfo.ArgumentList.Add("--artifacts-path=/tmp/artifacts/qbtmud");
                startInfo.Environment["ArtifactsPath"] = "/tmp/artifacts/qbtmud";
                startInfo.Environment["UseArtifactsOutput"] = "true";
                using var process = new Process { StartInfo = startInfo };
                process.Start().Should().BeTrue();
                var standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
                var standardError = process.StandardError.ReadToEndAsync(cancellationToken);
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromMinutes(2));

                try
                {
                    await process.WaitForExitAsync(timeout.Token);
                }
                finally
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                        using var cleanupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                        await process.WaitForExitAsync(cleanupTimeout.Token);
                    }

                    await Task.WhenAll(standardOutput, standardError);
                }

                process.ExitCode.Should().Be(0, "the actual translation target should succeed. Standard output: {0}. Standard error: {1}",
                    await standardOutput, await standardError);
                var generatedPath = Path.Combine(appDirectory, "wwwroot", "i18n", "webui_en.json");
                File.Exists(generatedPath).Should().BeTrue();
                using var generated = JsonDocument.Parse(await File.ReadAllTextAsync(generatedPath, cancellationToken));
                generated.RootElement.GetProperty("Context|Source").GetString().Should().Be("Translation");
                var manifestPath = Path.Combine(appDirectory, "wwwroot", "i18n", "webui_languages.json");
                using var manifest = JsonDocument.Parse(await File.ReadAllTextAsync(manifestPath, cancellationToken));
                manifest.RootElement.EnumerateArray().Select(locale => locale.GetString()).Should().Equal("en");
            }
            finally
            {
                try
                {
                    GetInputHashes(originalPaths).Should().BeEquivalentTo(originalHashes);
                }
                finally
                {
                    var absoluteScratchRoot = Path.GetFullPath(scratchRoot);
                    var absoluteTempRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath()));
                    Path.GetDirectoryName(absoluteScratchRoot).Should().Be(absoluteTempRoot);
                    Path.GetFileName(absoluteScratchRoot).Should().StartWith("qbtmud translation generation ");
                    Directory.Delete(absoluteScratchRoot, recursive: true);
                }
            }
        }

        private static Dictionary<string, string> GetInputHashes(IEnumerable<string> paths)
        {
            return paths.ToDictionary(path => path, path => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))), StringComparer.Ordinal);
        }

        private static string FindRepositoryRoot([CallerFilePath] string sourcePath = "")
        {
            var directory = Path.GetDirectoryName(sourcePath);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory, "Lantean.QBTMud.slnx")))
                {
                    return directory;
                }

                directory = Directory.GetParent(directory)?.FullName;
            }

            throw new DirectoryNotFoundException("The repository root could not be located from the test source path.");
        }
    }
}
