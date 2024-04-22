﻿namespace Lantean.QBTMudBlade.Models
{
    public enum Status
    {
        All,
        Downloading,
        Seeding,
        Completed,
        Resumed,
        Paused,
        Active,
        Inactive,
        Stalled,
        StalledUploading,
        StalledDownloading,
        Checking,
        Errored
    }
}