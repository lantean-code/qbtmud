namespace Lantean.QBTMud.Interop
{
    /// <summary>
    /// Represents a raw browser storage entry.
    /// </summary>
    /// <param name="Key">The storage key.</param>
    /// <param name="Value">The storage value.</param>
    public sealed record BrowserStorageEntry(string Key, string? Value);
}
