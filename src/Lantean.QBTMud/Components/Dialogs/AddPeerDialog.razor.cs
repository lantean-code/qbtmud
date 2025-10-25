using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class AddPeerDialog
    {
        [CascadingParameter]
        public IMudDialogInstance MudDialog { get; set; } = default!;

        protected HashSet<PeerId> Peers { get; } = [];

        protected string? IP { get; set; }

        protected int? Port { get; set; }

        protected void AddTracker()
        {
            if (string.IsNullOrEmpty(IP) || !Port.HasValue)
            {
                return;
            }
            Peers.Add(new PeerId(IP, Port.Value));
            IP = null;
            Port = null;
        }

        protected void SetIP(string value)
        {
            IP = value;
        }

        protected void SetPort(int? value)
        {
            Port = value;
        }

        protected void DeletePeer(PeerId peer)
        {
            Peers.Remove(peer);
        }

        protected void Cancel()
        {
            MudDialog.Cancel();
        }

        protected void Submit()
        {
            MudDialog.Close(Peers);
        }

        protected override Task Submit(KeyboardEvent keyboardEvent)
        {
            Submit();

            return Task.CompletedTask;
        }
    }
}