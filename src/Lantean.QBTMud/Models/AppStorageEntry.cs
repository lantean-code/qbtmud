namespace Lantean.QBTMud.Models
{
    /// <summary>
    /// Represents a qbtmud-prefixed local storage entry.
    /// </summary>
    public sealed record AppStorageEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppStorageEntry"/> class.
        /// </summary>
        /// <param name="key">The fully-prefixed storage key.</param>
        /// <param name="displayKey">The key displayed in the UI without the qbtmud prefix.</param>
        /// <param name="value">The raw value as stored in local storage.</param>
        /// <param name="preview">A shortened preview of the value.</param>
        /// <param name="length">The raw value length, in characters.</param>
        public AppStorageEntry(string key, string displayKey, string? value, string preview, int length)
        {
            Key = key;
            DisplayKey = displayKey;
            Value = value;
            Preview = preview;
            Length = length;
        }

        /// <summary>
        /// Gets the fully-prefixed storage key.
        /// </summary>
        public string Key { get; init; }

        /// <summary>
        /// Gets the key displayed in the UI without the qbtmud prefix.
        /// </summary>
        public string DisplayKey { get; init; }

        /// <summary>
        /// Gets the raw value as stored in local storage.
        /// </summary>
        public string? Value { get; init; }

        /// <summary>
        /// Gets a shortened preview of the value.
        /// </summary>
        public string Preview { get; init; }

        /// <summary>
        /// Gets the raw value length, in characters.
        /// </summary>
        public int Length { get; init; }
    }
}
