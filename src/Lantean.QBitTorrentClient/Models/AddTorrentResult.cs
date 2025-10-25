using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record AddTorrentResult
    {
        [JsonConstructor]
        public AddTorrentResult(int successCount, int failureCount, int pendingCount, IReadOnlyList<string>? addedTorrentIds)
        {
            SuccessCount = successCount;
            FailureCount = failureCount;
            PendingCount = pendingCount;
            AddedTorrentIds = addedTorrentIds ?? Array.Empty<string>();
        }

        [JsonPropertyName("success_count")]
        public int SuccessCount { get; }

        [JsonPropertyName("failure_count")]
        public int FailureCount { get; }

        [JsonPropertyName("pending_count")]
        public int PendingCount { get; }

        [JsonPropertyName("added_torrent_ids")]
        public IReadOnlyList<string> AddedTorrentIds { get; }
    }
}
