namespace Lantean.QBTMud.Application.Services
{
    public interface IClipboardService
    {
        Task WriteToClipboard(string text);
    }
}
