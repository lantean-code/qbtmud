using Lantean.QBTMudBlade.Models;

namespace Lantean.QBTMudBlade.Services
{
    public interface IKeyboardService
    {
        Task RegisterKeypressEvent(KeyboardEvent criteria, Func<KeyboardEvent, Task> onKeyPress);

        Task UnregisterKeypressEvent(KeyboardEvent criteria);
    }
}
