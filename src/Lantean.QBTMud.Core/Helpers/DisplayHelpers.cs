using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using ByteSizeLib;
using Lantean.QBTMud.Core.Models;
using MudBlazor;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Core.Helpers
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
                return string.Empty;
            }

            const long InfiniteEtaSentinelSeconds = 8_640_000; // ~100 days, used by qBittorrent for "infinite" ETA.
            var value = seconds.Value;

            if (value >= long.MaxValue || value >= TimeSpan.MaxValue.TotalSeconds || value == InfiniteEtaSentinelSeconds)
            {
                return "∞";
            }

            if (value <= 0)
            {
                return "< 1m";
            }

            var time = TimeSpan.FromSeconds(value);
            if (time.TotalMinutes < 1)
            {
                return "< 1m";
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
            stringBuilder.Append(FormatBinarySize(size.Value));
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

            return Size(size, prefix, suffix);
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
            stringBuilder.Append(FormatBinarySize(size.Value));
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
        public static string Percentage(double? value)
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

        public static string State(TorrentState? state)
        {
            var status = state switch
            {
                TorrentState.Downloading => "Downloading",
                TorrentState.StalledDownloading => "Stalled",
                TorrentState.DownloadingMetadata => "Downloading metadata",
                TorrentState.ForcedDownloadingMetadata => "[F] Downloading metadata",
                TorrentState.ForcedDownloading => "[F] Downloading",
                TorrentState.Uploading or TorrentState.StalledUploading => "Seeding",
                TorrentState.ForcedUploading => "[F] Seeding",
                TorrentState.QueuedDownloading or TorrentState.QueuedUploading => "Queued",
                TorrentState.CheckingDownloading or TorrentState.CheckingUploading => "Checking",
                TorrentState.CheckingResumeData => "Checking resume data",
                TorrentState.StoppedDownloading => "Stopped",
                TorrentState.StoppedUploading => "Completed",
                TorrentState.Moving => "Moving",
                TorrentState.MissingFiles => "Missing Files",
                TorrentState.Error => "Errored",
                _ => "Unknown",
            };

            return status;
        }

        public static (string, Color) GetStateIcon(TorrentState? state)
        {
            switch (state)
            {
                case TorrentState.ForcedDownloading:
                case TorrentState.DownloadingMetadata:
                case TorrentState.ForcedDownloadingMetadata:
                case TorrentState.Downloading:
                    return (Icons.Material.Filled.Downloading, Color.Success);

                case TorrentState.ForcedUploading:
                case TorrentState.Uploading:
                    return (Icons.Material.Filled.Upload, Color.Info);

                case TorrentState.StalledUploading:
                    return (Icons.Material.Filled.KeyboardDoubleArrowUp, Color.Info);

                case TorrentState.StalledDownloading:
                    return (Icons.Material.Filled.KeyboardDoubleArrowDown, Color.Success);

                case TorrentState.StoppedDownloading:
                    return (Icons.Material.Filled.Stop, Color.Success);

                case TorrentState.StoppedUploading:
                    return (Icons.Material.Filled.Stop, Color.Info);

                case TorrentState.QueuedDownloading:
                case TorrentState.QueuedUploading:
                    return (Icons.Material.Filled.Queue, Color.Default);

                case TorrentState.CheckingDownloading:
                case TorrentState.CheckingUploading:
                    return (Icons.Material.Filled.Loop, Color.Info);

                case TorrentState.CheckingResumeData:
                    return (Icons.Material.Filled.Loop, Color.Warning);

                case TorrentState.Moving:
                    return (Icons.Material.Filled.Moving, Color.Info);

                case TorrentState.Error:
                case TorrentState.Unknown:
                case TorrentState.MissingFiles:
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
                Status.Stopped => (Icons.Material.Filled.Stop, Color.Default),
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

        public static string Bool(bool value, string trueText = "Yes", string falseText = "No")
        {
            return value ? trueText : falseText;
        }

        public static string RatioLimit(double value)
        {
            if (value == Limits.UseGlobalShareRatioLimit)
            {
                return "Global";
            }

            if (value <= Limits.NoShareRatioLimit)
            {
                return "∞";
            }

            return value.ToString("0.00");
        }

        private static string FormatBinarySize(long size)
        {
            return ByteSize.FromBytes(size).ToString("#.##", CultureInfo.CurrentCulture, useBinaryByte: true);
        }
    }
}
