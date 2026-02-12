using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class LostConnectionDialog
    {
        [Inject]
        protected IWebUiLocalizer WebUiLocalizer { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        protected void Reconnect()
        {
            NavigationManager.NavigateTo(NavigationManager.BaseUri, forceLoad: true);
        }
    }
}
