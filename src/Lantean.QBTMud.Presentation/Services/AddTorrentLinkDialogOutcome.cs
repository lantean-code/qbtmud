namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Describes the outcome of adding torrents from the URL dialog.
    /// </summary>
    public enum AddTorrentLinkDialogOutcome
    {
        /// <summary>The user dismissed the dialog without adding torrents.</summary>
        Cancelled,

        /// <summary>The add request completed.</summary>
        Added,

        /// <summary>The add request was accepted and is still pending.</summary>
        Pending,

        /// <summary>The add request failed and can be retried.</summary>
        Failed,
    }
}
