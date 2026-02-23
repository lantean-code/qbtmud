using AwesomeAssertions;
using Lantean.QBitTorrentClient.Models;
using System.Net;

namespace Lantean.QBitTorrentClient.Test
{
    public class ApiClientTorrentCreatorTests
    {
        private readonly ApiClient _target;
        private readonly StubHttpMessageHandler _handler;

        public ApiClientTorrentCreatorTests()
        {
            _handler = new StubHttpMessageHandler();
            var http = new HttpClient(_handler) { BaseAddress = new Uri("http://localhost/") };
            _target = new ApiClient(http);
        }

        [Fact]
        public async Task GIVEN_NullRequest_WHEN_AddTorrentCreationTask_THEN_ShouldThrowArgumentNullException()
        {
            var act = async () => await _target.AddTorrentCreationTask(null!);

            var ex = await act.Should().ThrowAsync<ArgumentNullException>();
            ex.Which.ParamName.Should().Be("request");
        }

        [Fact]
        public async Task GIVEN_EmptySourcePath_WHEN_AddTorrentCreationTask_THEN_ShouldThrowArgumentException()
        {
            var act = async () => await _target.AddTorrentCreationTask(new TorrentCreationTaskRequest
            {
                SourcePath = " "
            });

            var ex = await act.Should().ThrowAsync<ArgumentException>();
            ex.Which.ParamName.Should().Be("request");
            ex.Which.Message.Should().Contain("SourcePath is required.");
        }

        [Fact]
        public async Task GIVEN_MinimalRequest_WHEN_AddTorrentCreationTask_THEN_ShouldPOSTOnlySourcePathAndReturnEmptyOnEmptyBody()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/torrentcreator/addTask");
                req.Content!.Headers.ContentType!.MediaType.Should().Be("application/x-www-form-urlencoded");

                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("sourcePath=%2Fsrc");

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(string.Empty)
                };
            };

            var id = await _target.AddTorrentCreationTask(new TorrentCreationTaskRequest
            {
                SourcePath = "/src"
            });

            id.Should().Be(string.Empty);
        }

        [Fact]
        public async Task GIVEN_AllFields_WHEN_AddTorrentCreationTask_THEN_ShouldIncludeEveryParameterAndReturnTaskId()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/torrentcreator/addTask");

                var form = await req.Content!.ReadAsStringAsync(ct);
                var parts = form.Split('&')
                    .Select(p => p.Split('='))
                    .ToDictionary(a => a[0], a => Uri.UnescapeDataString(a.Length > 1 ? a[1] : string.Empty));

                parts["sourcePath"].Should().Be("/src");
                parts["torrentFilePath"].Should().Be("/out.torrent");
                parts["pieceSize"].Should().Be("512");
                parts["private"].Should().Be("true");
                parts["startSeeding"].Should().Be("false");
                parts["comment"].Should().Be("hello");
                parts["source"].Should().Be("mysrc");
                parts["trackers"].Should().Be("t1|t2");
                parts["urlSeeds"].Should().Be("u1|u2");
                parts["format"].Should().Be("v2");
                parts["optimizeAlignment"].Should().Be("true");
                parts["paddedFileSizeLimit"].Should().Be("4096");

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"taskID\":\"task-123\"}")
                };
            };

            var request = new TorrentCreationTaskRequest
            {
                SourcePath = "/src",
                TorrentFilePath = "/out.torrent",
                PieceSize = 512,
                Private = true,
                StartSeeding = false,
                Comment = "hello",
                Source = "mysrc",
                Trackers = new[] { "t1", "t2" },
                UrlSeeds = new[] { "u1", "u2" },
                Format = "v2",
                OptimizeAlignment = true,
                PaddedFileSizeLimit = 4096
            };

            var id = await _target.AddTorrentCreationTask(request);

            id.Should().Be("task-123");
        }

        [Fact]
        public async Task GIVEN_OKButNoTaskIdInJson_WHEN_AddTorrentCreationTask_THEN_ShouldReturnEmptyString()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });

            var id = await _target.AddTorrentCreationTask(new TorrentCreationTaskRequest
            {
                SourcePath = "/src"
            });

            id.Should().Be(string.Empty);
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_AddTorrentCreationTask_THEN_ShouldThrowWithStatusAndMessage()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad req")
            });

            var act = async () => await _target.AddTorrentCreationTask(new TorrentCreationTaskRequest { SourcePath = "/src" });

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            ex.Which.Message.Should().Be("bad req");
        }

        [Fact]
        public async Task GIVEN_NoTaskId_WHEN_GetTorrentCreationTasks_THEN_ShouldGETWithoutQueryAndReturnList()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Get);
                req.RequestUri!.AbsolutePath.Should().Be("/torrentcreator/status");
                req.RequestUri!.Query.Should().BeEmpty();

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]")
                });
            };

            var list = await _target.GetTorrentCreationTasks();

            list.Should().NotBeNull();
            list.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_TaskId_WHEN_GetTorrentCreationTasks_THEN_ShouldGETWithQuery()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Get);
                req.RequestUri!.ToString().Should().Be("http://localhost/torrentcreator/status?taskID=task-1");

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]")
                });
            };

            var list = await _target.GetTorrentCreationTasks("task-1");

            list.Should().NotBeNull();
            list.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_TorrentCreationTaskStatusPayload_WHEN_GetTorrentCreationTasks_THEN_ShouldDeserializeTasks()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Get);
                req.RequestUri!.ToString().Should().Be("http://localhost/torrentcreator/status");

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                        [
                            {
                                "taskID": "TaskId",
                                "sourcePath": "/source",
                                "pieceSize": 512,
                                "private": true,
                                "timeAdded": "2024-01-01 00:00",
                                "format": "v2",
                                "optimizeAlignment": false,
                                "paddedFileSizeLimit": 4096,
                                "status": "Running",
                                "comment": "Comment",
                                "torrentFilePath": "/output.torrent",
                                "source": "Source",
                                "trackers": [ "t1", "t2" ],
                                "urlSeeds": [ "u1", "u2" ],
                                "timeStarted": "2024-01-01 00:01",
                                "timeFinished": "2024-01-01 00:02",
                                "errorMessage": "ErrorMessage",
                                "progress": 0.75
                            }
                        ]
                        """)
                });
            };

            var list = await _target.GetTorrentCreationTasks();

            list.Should().ContainSingle();
            list[0].TaskId.Should().Be("TaskId");
            list[0].SourcePath.Should().Be("/source");
            list[0].PieceSize.Should().Be(512);
            list[0].Private.Should().BeTrue();
            list[0].TimeAdded.Should().Be("2024-01-01 00:00");
            list[0].Format.Should().Be("v2");
            list[0].OptimizeAlignment.Should().BeFalse();
            list[0].PaddedFileSizeLimit.Should().Be(4096);
            list[0].Status.Should().Be("Running");
            list[0].Comment.Should().Be("Comment");
            list[0].TorrentFilePath.Should().Be("/output.torrent");
            list[0].Source.Should().Be("Source");
            list[0].Trackers.Should().BeEquivalentTo(new[] { "t1", "t2" });
            list[0].UrlSeeds.Should().BeEquivalentTo(new[] { "u1", "u2" });
            list[0].TimeStarted.Should().Be("2024-01-01 00:01");
            list[0].TimeFinished.Should().Be("2024-01-01 00:02");
            list[0].ErrorMessage.Should().Be("ErrorMessage");
            list[0].Progress.Should().Be(0.75);
        }

        [Fact]
        public async Task GIVEN_TorrentCreationTaskStatusWithNullLists_WHEN_GetTorrentCreationTasks_THEN_ShouldUseEmptyLists()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Get);
                req.RequestUri!.ToString().Should().Be("http://localhost/torrentcreator/status");

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                        [
                            {
                                "taskID": "TaskId",
                                "trackers": null,
                                "urlSeeds": null
                            }
                        ]
                        """)
                });
            };

            var list = await _target.GetTorrentCreationTasks();

            list.Should().ContainSingle();
            list[0].Trackers.Should().BeEmpty();
            list[0].UrlSeeds.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_BadJson_WHEN_GetTorrentCreationTasks_THEN_ShouldReturnEmptyList()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("oops")
            });

            var list = await _target.GetTorrentCreationTasks();

            list.Should().NotBeNull();
            list.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_GetTorrentCreationTasks_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("busy")
            });

            var act = async () => await _target.GetTorrentCreationTasks();

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            ex.Which.Message.Should().Be("busy");
        }

        [Fact]
        public async Task GIVEN_TaskId_WHEN_GetTorrentCreationTaskFile_THEN_ShouldGETWithQueryAndReturnBytes()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Get);
                req.RequestUri!.ToString().Should().Be("http://localhost/torrentcreator/torrentFile?taskID=abc");

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(new byte[] { 1, 2 })
                });
            };

            var bytes = await _target.GetTorrentCreationTaskFile("abc");

            bytes.Should().Equal(new byte[] { 1, 2 });
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_GetTorrentCreationTaskFile_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("missing")
            });

            var act = async () => await _target.GetTorrentCreationTaskFile("x");

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
            ex.Which.Message.Should().Be("missing");
        }

        [Fact]
        public async Task GIVEN_TaskId_WHEN_DeleteTorrentCreationTask_THEN_ShouldPOSTForm()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/torrentcreator/deleteTask");
                req.Content!.Headers.ContentType!.MediaType.Should().Be("application/x-www-form-urlencoded");

                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("taskID=abc");

                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.DeleteTorrentCreationTask("abc");
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_DeleteTorrentCreationTask_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent("nope")
            });

            var act = async () => await _target.DeleteTorrentCreationTask("abc");

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            ex.Which.Message.Should().Be("nope");
        }
    }
}
