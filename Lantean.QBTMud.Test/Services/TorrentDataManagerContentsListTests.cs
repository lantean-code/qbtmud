using AwesomeAssertions;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;

using MudPriority = Lantean.QBTMud.Models.Priority;
using QbtPriority = Lantean.QBitTorrentClient.Models.Priority;

namespace Lantean.QBTMud.Test.Services
{
    public class TorrentDataManagerContentsListTests
    {
        private readonly TorrentDataManager _target = new TorrentDataManager();

        // ---------------------------
        // CreateContentsList tests
        // ---------------------------

        [Fact]
        public void GIVEN_NoFiles_WHEN_CreateContentsList_THEN_ReturnsEmpty()
        {
            // arrange
            var files = Array.Empty<FileData>();

            // act
            var result = _target.CreateContentsList(files);

            // assert
            result.Should().NotBeNull();
            result.Count.Should().Be(0);
        }

        [Fact]
        public void GIVEN_SingleRootFile_WHEN_CreateContentsList_THEN_CreatesSingleFileNode()
        {
            // arrange
            var files = new[]
            {
                new FileData(
                    index: 5,
                    name: "file1.mkv",
                    size: 100,
                    progress: 0.5f,
                    priority: QbtPriority.Normal,
                    isSeed: false,
                    pieceRange: Array.Empty<int>(),
                    availability: 3.2f)
            };

            // act
            var result = _target.CreateContentsList(files);

            // assert
            result.Count.Should().Be(1);
            result.ContainsKey("file1.mkv").Should().BeTrue();

            var file = result["file1.mkv"];
            file.IsFolder.Should().BeFalse();
            file.Name.Should().Be("file1.mkv");
            file.DisplayName.Should().Be("file1.mkv");
            file.Index.Should().Be(5);
            file.Path.Should().Be("");      // root
            file.Level.Should().Be(0);
            file.Size.Should().Be(100);
            file.Progress.Should().Be(0.5f);
            file.Availability.Should().Be(3.2f);
            file.Priority.Should().Be(MudPriority.Normal);
        }

        [Fact]
        public void GIVEN_NestedFiles_AND_UnwantedFolder_WHEN_CreateContentsList_THEN_Skips_Unwanted_And_Aggregates()
        {
            // arrange
            var files = new[]
            {
        // ".unwanted" folder is skipped as a directory, but the file remains under "a"
        new FileData(10, "a/.unwanted/skip.bin", 10, 0.8f, QbtPriority.Normal, false, Array.Empty<int>(), 2.0f),
        new FileData(11, "a/b/c1.txt", 30, 0.4f, QbtPriority.High, false, Array.Empty<int>(), 1.0f),
        new FileData(12, "a/b/c2.txt", 70, 0.9f, QbtPriority.DoNotDownload, false, Array.Empty<int>(), 1.5f),
    };

            // act
            var result = _target.CreateContentsList(files);

            // assert: keys present
            result.ContainsKey("a").Should().BeTrue();
            result.ContainsKey("a/b").Should().BeTrue();
            result.ContainsKey("a/.unwanted/skip.bin").Should().BeTrue();
            result.ContainsKey("a/b/c1.txt").Should().BeTrue();
            result.ContainsKey("a/b/c2.txt").Should().BeTrue();

            // NOTE: CreateContentsList aggregates using TOTAL size as denominator (not "active" size).
            // For "a/b":
            //   size = 30 + 70 = 100
            //   progressSum = 0.4*30 (DND child excluded from sum) = 12
            //   availabilitySum = 1.0*30 = 30
            //   => progress = 12 / 100 = 0.12
            //   => availability = 30 / 100 = 0.3
            //   priority = Mixed (High vs DoNotDownload)
            var ab = result["a/b"];
            ab.IsFolder.Should().BeTrue();
            ab.Level.Should().Be(1);
            ab.Size.Should().Be(100);
            ab.Progress.Should().BeApproximately(0.12f, 1e-6f);
            ab.Availability.Should().BeApproximately(0.3f, 1e-6f);
            ab.Priority.Should().Be(MudPriority.Mixed);

            // For "a":
            // children: "a/.unwanted/skip.bin" (size=10, p=0.8, avail=2.0, Normal)
            //           "a/b" (size=100, p=0.12, avail=0.3, Mixed)
            //   size = 110
            //   progressSum = 0.8*10 + 0.12*100 = 8 + 12 = 20  => progress = 20/110 ≈ 0.181818...
            //   availabilitySum = 2.0*10 + 0.3*100 = 20 + 30 = 50 => availability = 50/110 ≈ 0.454545...
            //   priority = Mixed (Normal vs Mixed)
            var a = result["a"];
            a.IsFolder.Should().BeTrue();
            a.Level.Should().Be(0);
            a.Size.Should().Be(110);
            a.Progress.Should().BeApproximately(20f / 110f, 1e-6f);
            a.Availability.Should().BeApproximately(50f / 110f, 1e-6f);
            a.Priority.Should().Be(MudPriority.Mixed);

            // folder indices are less than min file index (10); deeper folder created later => smaller index
            a.Index.Should().BeLessThan(10);
            ab.Index.Should().BeLessThan(10);
            ab.Index.Should().BeLessThan(a.Index);
        }


        [Fact]
        public void GIVEN_AllChildrenDoNotDownload_WHEN_CreateContentsList_THEN_FolderProgressAndAvailabilityAreZero_AndPriorityDND()
        {
            // arrange
            var files = new[]
            {
                new FileData(4, "d/x.bin", 50, 0.5f, QbtPriority.DoNotDownload, false, Array.Empty<int>(), 0.9f),
                new FileData(5, "d/y.bin", 50, 1.2f, QbtPriority.DoNotDownload, false, Array.Empty<int>(), 1.1f)
            };

            // act
            var result = _target.CreateContentsList(files);

            // assert
            result.ContainsKey("d").Should().BeTrue();
            var d = result["d"];
            d.IsFolder.Should().BeTrue();
            d.Size.Should().Be(100);
            d.Progress.Should().Be(0f);      // activeSize == 0
            d.Availability.Should().Be(0f);  // activeSize == 0
            d.Priority.Should().Be(MudPriority.DoNotDownload);
        }

        // ---------------------------
        // MergeContentsList tests
        // ---------------------------

        [Fact]
        public void GIVEN_NoFiles_AND_EmptyContents_WHEN_MergeContentsList_THEN_ReturnsFalseAndNoChange()
        {
            // arrange
            var files = Array.Empty<FileData>();
            var contents = new Dictionary<string, ContentItem>();

            // act
            var changed = _target.MergeContentsList(files, contents);

            // assert
            changed.Should().BeFalse();
            contents.Count.Should().Be(0);
        }

        [Fact]
        public void GIVEN_NoFiles_AND_ExistingContents_WHEN_MergeContentsList_THEN_ClearsAndReturnsTrue()
        {
            // arrange
            var contents = new Dictionary<string, ContentItem>
            {
                ["folder"] = new ContentItem("folder", "folder", -1, MudPriority.Normal, 0, 0, 0, true, 0),
                ["folder/file.txt"] = new ContentItem("folder/file.txt", "file.txt", 10, MudPriority.Normal, 0.5f, 100, 1.0f, false, 1)
            };

            // act
            var changed = _target.MergeContentsList(Array.Empty<FileData>(), contents);

            // assert
            changed.Should().BeTrue();
            contents.Count.Should().Be(0);
        }

        [Fact]
        public void GIVEN_NewFilesOnly_WHEN_MergeContentsList_THEN_AddsFilesAndDirectoriesAndAggregates()
        {
            // arrange
            var files = new[]
            {
                new FileData(7, "root1.txt", 10, 0.6f, QbtPriority.Normal, false, Array.Empty<int>(), 2.0f),
                new FileData(8, "folder1/file1.bin", 30, 0.4f, QbtPriority.High, false, Array.Empty<int>(), 1.0f),
                new FileData(9, "folder1/file2.bin", 70, 0.9f, QbtPriority.DoNotDownload, false, Array.Empty<int>(), 1.5f)
            };
            var contents = new Dictionary<string, ContentItem>();

            // act
            var changed = _target.MergeContentsList(files, contents);

            // assert
            changed.Should().BeTrue();

            // keys present
            contents.ContainsKey("root1.txt").Should().BeTrue();
            contents.ContainsKey("folder1").Should().BeTrue();
            contents.ContainsKey("folder1/file1.bin").Should().BeTrue();
            contents.ContainsKey("folder1/file2.bin").Should().BeTrue();

            // directory indexes should be < min file index (7)
            contents["folder1"].Index.Should().BeLessThan(7);

            // aggregation on folder1 (same math as earlier)
            var folder = contents["folder1"];
            folder.IsFolder.Should().BeTrue();
            folder.Size.Should().Be(100);
            folder.Progress.Should().BeApproximately(0.4f, 1e-6f);
            folder.Availability.Should().BeApproximately(1.0f, 1e-6f);
            folder.Priority.Should().Be(MudPriority.Mixed);
        }

        [Fact]
        public void GIVEN_ExistingFile_WHEN_MergeContentsList_WithChanges_THEN_UpdatesInPlaceAndReturnsTrue()
        {
            // arrange
            var contents = new Dictionary<string, ContentItem>
            {
                // pre-existing directory with expected rollup for the prior file state
                ["folder"] = new ContentItem("folder", "folder", -100, MudPriority.Normal, 0.50f, 100, 1.20f, true, 0),
                // existing file which will be updated (progress delta > tolerance, size changed, availability delta > tolerance)
                ["folder/file.txt"] = new ContentItem("folder/file.txt", "file.txt", 10, MudPriority.Normal, 0.50f, 100, 1.20f, false, 1),
                // an extra entry that should be removed (not seen)
                ["old.bin"] = new ContentItem("old.bin", "old.bin", 5, MudPriority.Normal, 0.1f, 10, 1.0f, false, 0),
            };

            var files = new[]
            {
                // same path but changed values
                new FileData(10, "folder/file.txt", 120, 0.5003f /* > tol from 0.50 by 0.0003 */, QbtPriority.Normal, false, Array.Empty<int>(), 1.2003f)
            };

            // act
            var changed = _target.MergeContentsList(files, contents);

            // assert
            changed.Should().BeTrue();

            contents.ContainsKey("folder/file.txt").Should().BeTrue();
            contents.ContainsKey("folder").Should().BeTrue();
            contents.ContainsKey("old.bin").Should().BeFalse(); // removed

            var file = contents["folder/file.txt"];
            file.Size.Should().Be(120);
            file.Progress.Should().Be(0.5003f);
            file.Availability.Should().Be(1.2003f);

            // folder roll-up should update too:
            var folder = contents["folder"];
            folder.Size.Should().Be(120);
            folder.Progress.Should().BeApproximately(0.5003f, 1e-6f);
            folder.Availability.Should().BeApproximately(1.2003f, 1e-6f);
        }

        [Fact]
        public void GIVEN_ExistingItemsWithNoMaterialChangeWithinTolerance_WHEN_MergeContentsList_THEN_ReturnsFalse()
        {
            // arrange
            // file defines a folder rollup: size 100, progress 0.5000, availability 1.2000, Normal
            var contents = new Dictionary<string, ContentItem>
            {
                ["folder"] = new ContentItem("folder", "folder", -1, MudPriority.Normal, 0.5000f, 100, 1.2000f, true, 0),
                ["folder/file.txt"] = new ContentItem("folder/file.txt", "file.txt", 10, MudPriority.Normal, 0.5000f, 100, 1.2000f, false, 1)
            };
            var files = new[]
            {
                // diffs are within tolerance (0.0001f) -> should NOT update
                new FileData(10, "folder/file.txt", 100, 0.50005f, QbtPriority.Normal, false, Array.Empty<int>(), 1.20005f)
            };

            // act
            var changed = _target.MergeContentsList(files, contents);

            // assert
            changed.Should().BeFalse();

            // nothing should have moved
            contents["folder/file.txt"].Progress.Should().Be(0.5000f);
            contents["folder/file.txt"].Availability.Should().Be(1.2000f);
            contents["folder/file.txt"].Size.Should().Be(100);

            contents["folder"].Progress.Should().Be(0.5000f);
            contents["folder"].Availability.Should().Be(1.2000f);
            contents["folder"].Size.Should().Be(100);
        }

        [Fact]
        public void GIVEN_FileUnderUnwantedDirectory_WHEN_MergeContentsList_THEN_CreatesParentButSkipsUnwantedFolder()
        {
            // arrange
            var contents = new Dictionary<string, ContentItem>();
            var files = new[]
            {
                new FileData(20, "x/.unwanted/y.bin", 10, 0.8f, QbtPriority.Normal, false, Array.Empty<int>(), 2.0f)
            };

            // act
            var changed = _target.MergeContentsList(files, contents);

            // assert
            changed.Should().BeTrue();
            contents.ContainsKey("x").Should().BeTrue();
            contents.ContainsKey("x/.unwanted/y.bin").Should().BeTrue();
            contents.Keys.Any(k => k.Contains(".unwanted") && k.EndsWith('/')).Should().BeFalse(); // no explicit ".unwanted" directory key
        }

        [Fact]
        public void GIVEN_ProgressAboveOne_WHEN_MergeContentsList_THEN_DirectoryProgressIsClampedToOne()
        {
            // arrange
            var contents = new Dictionary<string, ContentItem>();
            var files = new[]
            {
                new FileData(2, "c/fileA", 10, 2.5f /* > 1 */, QbtPriority.Normal, false, Array.Empty<int>(), 3.0f)
            };

            // act
            var changed = _target.MergeContentsList(files, contents);

            // assert
            changed.Should().BeTrue();
            var dir = contents["c"];
            dir.Progress.Should().Be(1.0f); // clamped
            dir.Size.Should().Be(10);
        }

        [Fact]
        public void GIVEN_ProgressBelowZero_WHEN_MergeContentsList_THEN_DirectoryProgressIsClampedToZero()
        {
            // arrange
            var contents = new Dictionary<string, ContentItem>();
            var files = new[]
            {
                new FileData(3, "d/fileB", 10, -0.5f /* < 0 */, QbtPriority.Normal, false, Array.Empty<int>(), 3.0f)
            };

            // act
            var changed = _target.MergeContentsList(files, contents);

            // assert
            changed.Should().BeTrue();
            var dir = contents["d"];
            dir.Progress.Should().Be(0.0f); // clamped
            dir.Size.Should().Be(10);
        }

        [Fact]
        public void GIVEN_ExistingDirectories_THEN_NewFolderIndicesAreLessThanMinOfExistingAndIncoming()
        {
            // arrange
            // existing directory with index -2 and an existing independent file with index 50
            var contents = new Dictionary<string, ContentItem>
            {
                ["exist"] = new ContentItem("exist", "exist", -2, MudPriority.Normal, 0f, 0L, 0f, true, 0),
                ["exist/keep.txt"] = new ContentItem("exist/keep.txt", "keep.txt", 50, MudPriority.Normal, 0.1f, 1L, 0.2f, false, 1)
            };

            // incoming files have min index 5
            var files = new[]
            {
                new FileData(5, "new/f1", 10, 0.3f, QbtPriority.Normal, false, Array.Empty<int>(), 1.0f),
                new FileData(9, "new/f2", 15, 0.6f, QbtPriority.Normal, false, Array.Empty<int>(), 1.0f),
                new FileData(11, "exist/keep.txt", 1, 0.1f, QbtPriority.Normal, false, Array.Empty<int>(), 0.2f) // matches existing
            };

            // act
            var changed = _target.MergeContentsList(files, contents);

            // assert
            changed.Should().BeTrue();
            contents.ContainsKey("new").Should().BeTrue();

            var minExistingIndex = contents.Values.Where(c => !c.IsFolder).Select(c => c.Index).DefaultIfEmpty(int.MaxValue).Min();
            var newFolder = contents["new"];
            newFolder.IsFolder.Should().BeTrue();
            // index is computed from nextFolderIndex = Min(minExistingIndex, minFileIndex) - 1
            // minExistingIndex (files) = min(50, 11) = 11  → min with minFileIndex(=5) => 5, so folder index < 5
            newFolder.Index.Should().BeLessThan(5);
        }
    }
}
