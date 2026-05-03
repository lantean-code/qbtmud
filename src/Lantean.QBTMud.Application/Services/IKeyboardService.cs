using Lantean.QBTMud.Core.Models;

namespace Lantean.QBTMud.Application.Services
{
    public interface IKeyboardService
    {
        Task Focus();

        Task UnFocus();

        Task RegisterKeypressEvent(KeyboardEvent criteria, Func<KeyboardEvent, Task> onKeyPress);

        Task UnregisterKeypressEvent(KeyboardEvent criteria);
    }
}
