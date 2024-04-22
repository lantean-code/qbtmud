﻿namespace Lantean.QBTMudBlade.Models
{
    public record PeerList
    {
        public PeerList(Dictionary<string, Peer> peers)
        {
            Peers = peers;
        }

        public Dictionary<string, Peer> Peers { get; }
    }
}