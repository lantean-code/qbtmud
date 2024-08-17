using ByteSizeLib;
using Lantean.QBTMudBlade.Models;
using MudBlazor;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Lantean.QBTMudBlade.Helpers
{
    public static class DisplayHelpers
    {
        /// <summary>
        /// Formats a time period in seconds into an appropriate unit based on the size.
        /// </summary>
        /// <param name="seconds"></param>
        /// <param name="prefix"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public static string Duration(long? seconds, string? prefix = null, string? suffix = null)
        {
            if (seconds is null)
            {
                return "";
            }

            if (seconds == 8640000)
            {
                return "∞";
            }

            if (seconds < 60)
            {
                return "< 1m";
            }

            TimeSpan time;
            try
            {
                time = TimeSpan.FromSeconds(seconds.Value);
            }
            catch (OverflowException)
            {
                return "∞";
            }
            var sb = new StringBuilder();
            if (prefix is not null)
            {
                sb.Append(prefix);
            }
            if (time.Days > 0)
            {
                sb.Append(time.Days);
                sb.Append('d');

                if (time.Hours != 0)
                {
                    sb.Append(' ');
                    sb.Append(time.Hours);
                    sb.Append('h');
                }
            }
            else if (time.Hours > 0)
            {
                sb.Append(time.Hours);
                sb.Append('h');

                if (time.Minutes != 0)
                {
                    sb.Append(' ');
                    sb.Append(time.Minutes);
                    sb.Append('m');
                }
            }
            else
            {
                sb.Append(time.Minutes);
                sb.Append('m');
            }
            if (suffix is not null)
            {
                sb.Append(' ');
                sb.Append(suffix);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Formats a file size in bytes into an appropriate unit based on the size.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="prefix"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public static string Size(long? size, string? prefix = null, string? suffix = null)
        {
            if (size is null)
            {
                return "";
            }

            if (size < 0)
            {
                size = 0;
            }

            var stringBuilder = new StringBuilder();
            if (prefix is not null)
            {
                stringBuilder.Append(prefix);
            }
            stringBuilder.Append(ByteSize.FromBytes(size.Value).ToString("#.##"));
            if (suffix is not null)
            {
                stringBuilder.Append(suffix);
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Formats a file size in bytes into an appropriate unit based on the size.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="prefix"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public static string Size(object? sizeValue, string? prefix = null, string? suffix = null)
        {
            if (sizeValue is not long size)
            {
                return "";
            }

            return Size(size);
        }

        /// <summary>
        /// Formats a transfer speed in bytes/s into an appropriate unit based on the size.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="prefix"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public static string Speed(long? size, string? prefix = null, string? suffix = null)
        {
            if (size is null)
            {
                return "";
            }

            if (size == -1)
            {
                return "∞";
            }

            var stringBuilder = new StringBuilder();
            if (prefix is not null)
            {
                stringBuilder.Append(prefix);
            }
            stringBuilder.Append(ByteSize.FromBytes(size.Value).ToString("#.##"));
            stringBuilder.Append("/s");
            if (suffix is not null)
            {
                stringBuilder.Append(suffix);
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Formats a value into an empty string if null, otherwise the value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="prefix"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public static string EmptyIfNull<T>(T? value, string? prefix = null, string? suffix = null, [StringSyntax("NumericFormat")] string? format = null) where T : struct, IConvertible
        {
            if (value is null)
            {
                return "";
            }

            var stringBuilder = new StringBuilder();
            if (prefix is not null)
            {
                stringBuilder.Append(prefix);
            }

            if (format is not null)
            {
                if (value is long longValue)
                {
                    stringBuilder.Append(longValue.ToString(format));
                }
                else if (value is int intValue)
                {
                    stringBuilder.Append(intValue.ToString(format));
                }
                else if (value is float floatValue)
                {
                    stringBuilder.Append(floatValue.ToString(format));
                }
                else if (value is double doubleValue)
                {
                    stringBuilder.Append(doubleValue.ToString(format));
                }
                else if (value is decimal decimalValue)
                {
                    stringBuilder.Append(decimalValue.ToString(format));
                }
                else if (value is short shortValue)
                {
                    stringBuilder.Append(shortValue.ToString(format));
                }
                else
                {
                    stringBuilder.Append(value.Value);
                }
            }
            else
            {
                stringBuilder.Append(value.Value);
            }

            if (suffix is not null)
            {
                stringBuilder.Append(suffix);
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Formats a value into an empty string if null, otherwise the value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="prefix"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public static string EmptyIfNull(string? value, string? prefix = null, string? suffix = null)
        {
            if (value is null)
            {
                return "";
            }

            var stringBuilder = new StringBuilder();
            if (prefix is not null)
            {
                stringBuilder.Append(prefix);
            }
            stringBuilder.Append(value);
            if (suffix is not null)
            {
                stringBuilder.Append(suffix);
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Formats a unix time (in seconds) into a local date time.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="negativeDescription"></param>
        /// <returns></returns>
        public static string DateTime(long? value, string negativeDescription = "")
        {
            if (value is null)
            {
                return "";
            }

            if (value.Value == -1)
            {
                return negativeDescription;
            }

            var dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(value.Value);

            return dateTimeOffset.ToLocalTime().DateTime.ToString();
        }

        /// <summary>
        /// Formats a value into a percentage or empty string if null.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Percentage(float? value)
        {
            if (value is null)
            {
                return "";
            }

            if (value < 0)
            {
                value = 0;
            }

            if (value == 0)
            {
                return "0%";
            }

            return value.Value.ToString("0.#%");
        }

        public static string State(string? state)
        {
            var status = state switch
            {
                "downloading" => "Downloading",
                "stalledDL" => "Stalled",
                "metaDL" => "Downloading metadata",
                "forcedMetaDL" => "[F] Downloading metadata",
                "forcedDL" => "[F] Downloading",
                "uploading" or "stalledUP" => "Seeding",
                "forcedUP" => "[F] Seeding",
                "queuedDL" or "queuedUP" => "Queued",
                "checkingDL" or "checkingUP" => "Checking",
                "queuedForChecking" => "Queued for checking",
                "checkingResumeData" => "Checking resume data",
                "pausedDL" => "Paused",
                "pausedUP" => "Completed",
                "moving" => "Moving",
                "missingFiles" => "Missing Files",
                "error" => "Errored",
                _ => "Unknown",
            };

            return status;
        }

        public static (string, Color) GetStateIcon(string? state)
        {
            switch (state)
            {
                case "forcedDL":
                case "metaDL":
                case "forcedMetaDL":
                case "downloading":
                    return (Icons.Material.Filled.Downloading, Color.Success);

                case "forcedUP":
                case "uploading":
                    return (Icons.Material.Filled.Upload, Color.Info);

                case "stalledUP":
                    return (Icons.Material.Filled.KeyboardDoubleArrowUp, Color.Info);

                case "stalledDL":
                    return (Icons.Material.Filled.KeyboardDoubleArrowDown, Color.Success);

                case "pausedDL":
                    return (Icons.Material.Filled.Pause, Color.Success);

                case "pausedUP":
                    return (Icons.Material.Filled.Pause, Color.Info);

                case "queuedDL":
                case "queuedUP":
                    return (Icons.Material.Filled.Queue, Color.Default);

                case "checkingDL":
                case "checkingUP":
                    return (Icons.Material.Filled.Loop, Color.Info);

                case "queuedForChecking":
                case "checkingResumeData":
                    return (Icons.Material.Filled.Loop, Color.Warning);

                case "moving":
                    return (Icons.Material.Filled.Moving, Color.Info);

                case "error":
                case "unknown":
                case "missingFiles":
                    return (Icons.Material.Filled.Error, Color.Error);

                default:
                    return (Icons.Material.Filled.QuestionMark, Color.Warning);
            }
        }

        public static (string, Color) GetStatusIcon(string statusValue)
        {
            var status = Enum.Parse<Status>(statusValue);
            return GetStatusIcon(status);
        }

        private static (string, Color) GetStatusIcon(Status status)
        {
            return status switch
            {
                Status.All => (Icons.Material.Filled.AllOut, Color.Warning),
                Status.Downloading => (Icons.Material.Filled.Downloading, Color.Success),
                Status.Seeding => (Icons.Material.Filled.Upload, Color.Info),
                Status.Completed => (Icons.Material.Filled.Check, Color.Default),
                Status.Resumed => (Icons.Material.Filled.PlayArrow, Color.Success),
                Status.Paused => (Icons.Material.Filled.Pause, Color.Default),
                Status.Active => (Icons.Material.Filled.Sort, Color.Success),
                Status.Inactive => (Icons.Material.Filled.Sort, Color.Error),
                Status.Stalled => (Icons.Material.Filled.Sort, Color.Info),
                Status.StalledUploading => (Icons.Material.Filled.KeyboardDoubleArrowUp, Color.Info),
                Status.StalledDownloading => (Icons.Material.Filled.KeyboardDoubleArrowDown, Color.Success),
                Status.Checking => (Icons.Material.Filled.Loop, Color.Info),
                Status.Errored => (Icons.Material.Filled.Error, Color.Error),
                _ => (Icons.Material.Filled.QuestionMark, Color.Inherit),
            };
        }
    }
}