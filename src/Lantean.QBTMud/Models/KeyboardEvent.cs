using System.Text.Json.Serialization;

namespace Lantean.QBTMud.Models
{
    public class KeyboardEvent
    {
        public KeyboardEvent(string key)
        {
            Key = key;
            Code = key;
        }

        [JsonConstructor]
        public KeyboardEvent(string key, bool repeat, bool ctrlKey, bool shiftKey, bool altKey, bool metaKey, string code) : this(key)
        {
            Repeat = repeat;
            CtrlKey = ctrlKey;
            ShiftKey = shiftKey;
            AltKey = altKey;
            MetaKey = metaKey;
            Code = code;
        }

        /// <summary>
        /// The key value of the key represented by the event.
        /// If the value has a printed representation, this attribute's value is the same as the char attribute.
        /// Otherwise, it's one of the key value strings specified in 'Key values'.
        /// If the key can't be identified, this is the string "Unidentified"
        /// </summary>
        [JsonPropertyName("key")]
        public string Key { get; set; }

        /// <summary>
        /// true if a key has been depressed long enough to trigger key repetition, otherwise false.
        /// </summary>
        [JsonPropertyName("repeat")]
        public bool Repeat { get; set; }

        /// <summary>
        /// true if the control key was down when the event was fired. false otherwise.
        /// </summary>
        [JsonPropertyName("ctrlKey")]
        public bool CtrlKey { get; set; }

        /// <summary>
        /// true if the shift key was down when the event was fired. false otherwise.
        /// </summary>
        [JsonPropertyName("shiftKey")]
        public bool ShiftKey { get; set; }

        /// <summary>
        /// true if the alt key was down when the event was fired. false otherwise.
        /// </summary>
        [JsonPropertyName("altKey")]
        public bool AltKey { get; set; }

        /// <summary>
        /// true if the meta key was down when the event was fired. false otherwise.
        /// </summary>
        [JsonPropertyName("metaKey")]
        public bool MetaKey { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; }

        public override bool Equals(object? obj)
        {
            return obj is KeyboardEvent @event &&
                   Key == @event.Key &&
                   Repeat == @event.Repeat &&
                   CtrlKey == @event.CtrlKey &&
                   ShiftKey == @event.ShiftKey &&
                   AltKey == @event.AltKey &&
                   MetaKey == @event.MetaKey;
        }

        public override string? ToString()
        {
            var modifiers = (CtrlKey ? "Ctrl" : "") + (ShiftKey ? "Shift" : "") + (AltKey ? "Alt" : "") + (MetaKey ? "Meta" : "");

            return modifiers + (modifiers.Length == 0 ? "" : "+") + (Key == "+" ? "'+'" : "+") + (Repeat ? "-repeated" : "");
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Key, Repeat, CtrlKey, ShiftKey, AltKey, MetaKey, Code);
        }

        public static implicit operator KeyboardEvent(string input)
        {
            return new KeyboardEvent(input);
        }

        public static implicit operator string(KeyboardEvent input)
        {
            return input.ToString()!;
        }
    }
}