using AwesomeAssertions;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using System.Reflection;

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
            result["FileA"].NewName.Should().Be("DocAlpha.txt");
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
            result["Report"].NewName.Should().Be("report.XxX");
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

            var renamed = result["File1"].NewName;
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

            var renamed = result["LongFile"].NewName!;
            renamed.Length.Should().Be(longName.Length);
            renamed.Take(250).Should().AllSatisfy(c => c.Should().Be('b'));
            renamed.Skip(250).Should().AllSatisfy(c => c.Should().Be('a'));
        }

        [Fact]
        public void GIVEN_StartBeforeZero_WHEN_ReplaceBetweenInvoked_THEN_ShouldReturnOriginal()
        {
            var method = typeof(FileNameMatcher).GetMethod("ReplaceBetween", BindingFlags.NonPublic | BindingFlags.Static)!;
            var result = (string)method.Invoke(null, new object[] { "sample", -1, 3, "X" })!;
            result.Should().Be("sample");
        }

        [Fact]
        public void GIVEN_EndBeyondLength_WHEN_ReplaceBetweenInvoked_THEN_ShouldReturnOriginal()
        {
            var method = typeof(FileNameMatcher).GetMethod("ReplaceBetween", BindingFlags.NonPublic | BindingFlags.Static)!;
            var result = (string)method.Invoke(null, new object[] { "sample", 2, 20, "X" })!;
            result.Should().Be("sample");
        }

        [Fact]
        public void GIVEN_StartGreaterThanEnd_WHEN_ReplaceBetweenInvoked_THEN_ShouldReturnOriginal()
        {
            var method = typeof(FileNameMatcher).GetMethod("ReplaceBetween", BindingFlags.NonPublic | BindingFlags.Static)!;
            var result = (string)method.Invoke(null, new object[] { "sample", 4, 3, "X" })!;
            result.Should().Be("sample");
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
