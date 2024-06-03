namespace Lantean.QBTMudBlade.Services
{
    public interface IClipboardService
    {
        Task WriteToClipboard(string text);
    }
}