namespace Lantean.QBTMud.Services
{
    public interface IClipboardService
    {
        Task WriteToClipboard(string text);
    }
}