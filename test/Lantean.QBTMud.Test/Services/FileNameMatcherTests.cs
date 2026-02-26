using AwesomeAssertions;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;

namespace Lantean.QBTMud.Test.Services
{
    public class FileNameMatcherTests
    {
        [Fact]
        public void GIVEN_EmptySearch_WHEN_GetRenamedFiles_THEN_ShouldReturnEmpty()
        {
            var files = new[]
            {
                CreateFile("FileA", "FileA.txt")
            };

            var result = FileNameMatcher.GetRenamedFiles(
                files,
                string.Empty,
                false,
                "new",
                false,
                false,
                AppliesTo.FilenameExtension,
                true,
                false,
                false,
                0);

            result.Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_FileMatch_WHEN_SimpleReplacement_THEN_ShouldRenameOnce()
        {
            var rows = new[]
            {
                CreateFile("FileA", "FileAlpha.txt"),
                CreateFolder("Folder", "Folder")
            };

            var result = FileNameMatcher.GetRenamedFiles(
                rows,
                "file",
                false,
                "Doc",
                false,
                false,
                AppliesTo.FilenameExtension,
                true,
                false,
                false,
                5);

            result.Should().HaveCount(1);
            result[0].NewName.Should().Be("DocAlpha.txt");
        }

        [Fact]
        public void GIVEN_MultipleMatches_AND_ReplaceAllFalse_WHEN_GetRenamedFiles_THEN_ShouldRenameOnlyFirst()
        {
            var rows = new[]
            {
                CreateFile("First", "first.txt"),
                CreateFile("Second", "first.txt")
            };

            var result = FileNameMatcher.GetRenamedFiles(
                rows,
                "first",
                false,
                "renamed",
                false,
                false,
                AppliesTo.FilenameExtension,
                true,
                false,
                false,
                0);

            result.Should().HaveCount(2);
            result[0].Name.Should().Be("First");
            result[0].NewName.Should().Be("renamed.txt");
            result[1].NewName.Should().Be("renamed.txt");
        }

        [Fact]
        public void GIVEN_MultipleMatches_AND_ReplaceAllTrue_WHEN_GetRenamedFiles_THEN_ShouldRenameAll()
        {
            var rows = new[]
            {
                CreateFile("First", "first.txt"),
                CreateFile("Second", "first.txt")
            };

            var result = FileNameMatcher.GetRenamedFiles(
                rows,
                "first",
                false,
                "renamed",
                false,
                false,
                AppliesTo.FilenameExtension,
                true,
                false,
                true,
                0);

            result.Should().HaveCount(2);
            result[0].Name.Should().Be("First");
            result[0].NewName.Should().Be("renamed.txt");
            result[1].Name.Should().Be("Second");
            result[1].NewName.Should().Be("renamed.txt");
        }

        [Fact]
        public void GIVEN_FileExcluded_WHEN_GetRenamedFiles_THEN_ShouldSkipDueToIncludeFiles()
        {
            var rows = new[]
            {
                CreateFile("FileA", "Example.txt")
            };

            var result = FileNameMatcher.GetRenamedFiles(
                rows,
                "Example",
                false,
                "Sample",
                false,
                false,
                AppliesTo.FilenameExtension,
                false,
                true,
                false,
                0);

            result.Should().BeEmpty();
            rows[0].NewName.Should().BeNull();
        }

        [Fact]
        public void GIVEN_CaseSensitiveMismatch_WHEN_GetRenamedFiles_THEN_ShouldNotRename()
        {
            var rows = new[]
            {
                CreateFile("FileA", "FileAlpha.txt")
            };

            var result = FileNameMatcher.GetRenamedFiles(
                rows,
                "file",
                false,
                "Doc",
                false,
                true,
                AppliesTo.FilenameExtension,
                true,
                false,
                false,
                0);

            result.Should().BeEmpty();
            rows[0].NewName.Should().BeNull();
        }

        [Fact]
        public void GIVEN_ExtensionTarget_WHEN_MatchAllOccurrences_THEN_ShouldRespectOffset()
        {
            var rows = new[]
            {
                CreateFile("Report", "report.txt")
            };

            var result = FileNameMatcher.GetRenamedFiles(
                rows,
                "t",
                false,
                "X",
                true,
                false,
                AppliesTo.Extension,
                true,
                false,
                false,
                0);

            result.Should().HaveCount(1);
            result[0].NewName.Should().Be("report.XxX");
        }

        [Fact]
        public void GIVEN_RegexWithGroups_WHEN_ReplacementContainsGroups_THEN_ShouldExpandVariables()
        {
            var rows = new[]
            {
                CreateFile("File1", "123-file.txt")
            };

            var result = FileNameMatcher.GetRenamedFiles(
                rows,
                @"(?<digits>\d+)-(?<name>file)",
                true,
                @"\prefix-$0-$digits-\$digits-$ddd$",
                false,
                false,
                AppliesTo.FilenameExtension,
                true,
                false,
                false,
                42);

            var renamed = result[0].NewName;
            renamed.Should().Be(@"\prefix-123-file-123-$digits-042.txt");
        }

        [Fact]
        public void GIVEN_MatchAllOccurrencesAboveLimit_WHEN_GetRenamedFiles_THEN_ShouldCapAt250()
        {
            var longName = new string('a', 300);
            var rows = new[]
            {
                CreateFile("LongFile", longName)
            };

            var result = FileNameMatcher.GetRenamedFiles(
                rows,
                "a",
                false,
                "b",
                true,
                false,
                AppliesTo.Filename,
                true,
                false,
                false,
                0);

            var renamed = result[0].NewName!;
            renamed.Length.Should().Be(longName.Length);
            renamed.Take(250).Should().AllSatisfy(c => c.Should().Be('b'));
            renamed.Skip(250).Should().AllSatisfy(c => c.Should().Be('a'));
        }

        [Fact]
        public void GIVEN_InvalidRegex_WHEN_GetRenamedFiles_THEN_ShouldReturnEmpty()
        {
            var rows = new[]
            {
                CreateFile("File", "File.txt")
            };

            var result = FileNameMatcher.GetRenamedFiles(
                rows,
                "(",
                true,
                "Replacement",
                false,
                false,
                AppliesTo.FilenameExtension,
                true,
                false,
                false,
                0);

            result.Should().BeEmpty();
            rows[0].NewName.Should().BeNull();
        }

        [Fact]
        public void GIVEN_RegexWithEmptyGroup_WHEN_GroupValueIsEmpty_THEN_ShouldSkipEmptyGroupReplacement()
        {
            var rows = new[]
            {
                CreateFile("File", "File.txt")
            };

            var result = FileNameMatcher.GetRenamedFiles(
                rows,
                "()",
                true,
                "X",
                false,
                false,
                AppliesTo.FilenameExtension,
                true,
                false,
                false,
                0);

            result.Should().HaveCount(1);
            result[0].NewName.Should().Be("XFile.txt");
        }

        [Fact]
        public void GIVEN_CatastrophicRegex_WHEN_TimeoutOccurs_THEN_ShouldReturnEmpty()
        {
            var longName = new string('a', 50000) + "b";
            var rows = new[]
            {
                CreateFile("Long", longName)
            };

            var result = FileNameMatcher.GetRenamedFiles(
                rows,
                "^(a+)+$",
                true,
                "Replacement",
                false,
                false,
                AppliesTo.Filename,
                true,
                false,
                false,
                0);

            result.Should().BeEmpty();
            rows[0].NewName.Should().BeNull();
        }

        [Fact]
        public void GIVEN_MatchOptions_WHEN_Created_THEN_DefaultsAreSet()
        {
            var options = new MatchOptions();

            options.MatchAllOccurrences.Should().BeFalse();
            options.CaseSensitive.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_ReplaceOptions_WHEN_Created_THEN_DefaultsAreSet()
        {
            var options = new ReplaceOptions();

            options.AppliesToOption.Should().Be(AppliesTo.FilenameExtension);
            options.IncludeFiles.Should().BeTrue();
            options.IncludeFolders.Should().BeFalse();
            options.ReplaceAll.Should().BeFalse();
            options.FileEnumerationStart.Should().Be(0);
        }

        private static FileRow CreateFile(string name, string originalName)
        {
            return new FileRow
            {
                Name = name,
                OriginalName = originalName,
                Path = $"/root/{name}",
                IsFolder = false
            };
        }

        private static FileRow CreateFolder(string name, string originalName)
        {
            var row = CreateFile(name, originalName);
            row.IsFolder = true;
            return row;
        }
    }
}
