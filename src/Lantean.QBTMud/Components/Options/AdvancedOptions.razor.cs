using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMud.Components.Options
{
    public partial class AdvancedOptions : Options
    {
        [Inject]
        public IApiClient ApiClient { get; set; } = default!;

        protected string? ResumeDataStorageType { get; private set; }
        protected int MemoryWorkingSetLimit { get; private set; }
        protected string? CurrentNetworkInterface { get; private set; }
        protected string? CurrentInterfaceAddress { get; private set; }
        protected int SaveResumeDataInterval { get; private set; }
        protected int TorrentFileSizeLimit { get; private set; }
        protected bool? RecheckCompletedTorrents { get; private set; }

        protected bool? ConfirmTorrentRecheck { get; private set; }
        protected string? AppInstanceName { get; private set; }
        protected int RefreshInterval { get; private set; }
        protected bool? ResolvePeerCountries { get; private set; }
        protected bool? ReannounceWhenAddressChanged { get; private set; }
        protected int BdecodeDepthLimit { get; private set; }
        protected int BdecodeTokenLimit { get; private set; }
        protected int AsyncIoThreads { get; private set; }
        protected int HashingThreads { get; private set; }
        protected int FilePoolSize { get; private set; }
        protected int CheckingMemoryUse { get; private set; }
        protected int DiskCache { get; private set; }
        protected int DiskCacheTtl { get; private set; }
        protected int DiskQueueSize { get; private set; }
        protected int DiskIoType { get; private set; }
        protected int DiskIoReadMode { get; private set; }
        protected int DiskIoWriteMode { get; private set; }
        protected bool? EnableCoalesceReadWrite { get; private set; }
        protected bool? EnablePieceExtentAffinity { get; private set; }
        protected bool? EnableUploadSuggestions { get; private set; }
        protected int SendBufferWatermark { get; private set; }
        protected int SendBufferLowWatermark { get; private set; }
        protected int SendBufferWatermarkFactor { get; private set; }
        protected int ConnectionSpeed { get; private set; }
        protected int SocketSendBufferSize { get; private set; }
        protected int SocketReceiveBufferSize { get; private set; }
        protected int SocketBacklogSize { get; private set; }
        protected int OutgoingPortsMin { get; private set; }
        protected int OutgoingPortsMax { get; private set; }
        protected int UpnpLeaseDuration { get; private set; }
        protected int PeerTos { get; private set; }
        protected int UtpTcpMixedMode { get; private set; }
        protected bool? IdnSupportEnabled { get; private set; }
        protected bool? EnableMultiConnectionsFromSameIp { get; private set; }
        protected bool? ValidateHttpsTrackerCertificate { get; private set; }
        protected bool? SsrfMitigation { get; private set; }
        protected bool? BlockPeersOnPrivilegedPorts { get; private set; }
        protected bool? EnableEmbeddedTracker { get; private set; }
        protected int EmbeddedTrackerPort { get; private set; }
        protected bool? EmbeddedTrackerPortForwarding { get; private set; }
        protected bool MarkOfTheWeb { get; private set; }
        protected string? PythonExecutablePath { get; private set; }
        protected int UploadSlotsBehavior { get; private set; }
        protected int UploadChokingAlgorithm { get; private set; }
        protected bool? AnnounceToAllTrackers { get; private set; }
        protected bool? AnnounceToAllTiers { get; private set; }
        protected string? AnnounceIp { get; private set; }
        protected int MaxConcurrentHttpAnnounces { get; private set; }
        protected int StopTrackerTimeout { get; private set; }
        protected int PeerTurnover { get; private set; }
        protected int PeerTurnoverCutoff { get; private set; }
        protected int PeerTurnoverInterval { get; private set; }
        protected int RequestQueueSize { get; private set; }
        protected string? DhtBootstrapNodes { get; private set; }
        protected int I2pInboundQuantity { get; private set; }
        protected int I2pOutboundQuantity { get; private set; }
        protected int I2pInboundLength { get; private set; }
        protected int I2pOutboundLength { get; private set; }

        protected IReadOnlyList<NetworkInterface> NetworkInterfaces { get; private set; } = [];

        protected IReadOnlyList<string> NetworkInterfaceAddresses { get; private set; } = [];

        protected override async Task OnInitializedAsync()
        {
            NetworkInterfaces = await ApiClient.GetNetworkInterfaces();
        }

        protected override bool SetOptions()
        {
            if (Preferences is null)
            {
                return false;
            }

            ResumeDataStorageType = Preferences.ResumeDataStorageType;
            MemoryWorkingSetLimit = Preferences.MemoryWorkingSetLimit;
            CurrentNetworkInterface = Preferences.CurrentNetworkInterface;
            CurrentInterfaceAddress = Preferences.CurrentInterfaceAddress;
            SaveResumeDataInterval = Preferences.SaveResumeDataInterval;
            TorrentFileSizeLimit = Preferences.TorrentFileSizeLimit / 1024 / 1024;
            RecheckCompletedTorrents = Preferences.RecheckCompletedTorrents;
            ConfirmTorrentRecheck = Preferences.ConfirmTorrentRecheck;
            AppInstanceName = Preferences.AppInstanceName;
            RefreshInterval = Preferences.RefreshInterval;
            ResolvePeerCountries = Preferences.ResolvePeerCountries;
            ReannounceWhenAddressChanged = Preferences.ReannounceWhenAddressChanged;
            BdecodeDepthLimit = Preferences.BdecodeDepthLimit;
            BdecodeTokenLimit = Preferences.BdecodeTokenLimit;
            AsyncIoThreads = Preferences.AsyncIoThreads;
            HashingThreads = Preferences.HashingThreads;
            FilePoolSize = Preferences.FilePoolSize;
            CheckingMemoryUse = Preferences.CheckingMemoryUse;
            DiskCache = Preferences.DiskCache;
            DiskCacheTtl = Preferences.DiskCacheTtl;
            DiskQueueSize = Preferences.DiskQueueSize / 1024;
            DiskIoType = Preferences.DiskIoType;
            DiskIoReadMode = Preferences.DiskIoReadMode;
            DiskIoWriteMode = Preferences.DiskIoWriteMode;
            EnableCoalesceReadWrite = Preferences.EnableCoalesceReadWrite;
            EnablePieceExtentAffinity = Preferences.EnablePieceExtentAffinity;
            EnableUploadSuggestions = Preferences.EnableUploadSuggestions;
            SendBufferWatermark = Preferences.SendBufferWatermark;
            SendBufferLowWatermark = Preferences.SendBufferLowWatermark;
            SendBufferWatermarkFactor = Preferences.SendBufferWatermarkFactor;
            ConnectionSpeed = Preferences.ConnectionSpeed;
            SocketSendBufferSize = Preferences.SocketSendBufferSize / 1024;
            SocketReceiveBufferSize = Preferences.SocketReceiveBufferSize / 1024;
            SocketBacklogSize = Preferences.SocketBacklogSize;
            OutgoingPortsMin = Preferences.OutgoingPortsMin;
            OutgoingPortsMax = Preferences.OutgoingPortsMax;
            UpnpLeaseDuration = Preferences.UpnpLeaseDuration;
            PeerTos = Preferences.PeerTos;
            UtpTcpMixedMode = Preferences.UtpTcpMixedMode;
            IdnSupportEnabled = Preferences.IdnSupportEnabled;
            EnableMultiConnectionsFromSameIp = Preferences.EnableMultiConnectionsFromSameIp;
            ValidateHttpsTrackerCertificate = Preferences.ValidateHttpsTrackerCertificate;
            SsrfMitigation = Preferences.SsrfMitigation;
            BlockPeersOnPrivilegedPorts = Preferences.BlockPeersOnPrivilegedPorts;
            EnableEmbeddedTracker = Preferences.EnableEmbeddedTracker;
            EmbeddedTrackerPort = Preferences.EmbeddedTrackerPort;
            EmbeddedTrackerPortForwarding = Preferences.EmbeddedTrackerPortForwarding;
            MarkOfTheWeb = Preferences.MarkOfTheWeb;
            PythonExecutablePath = Preferences.PythonExecutablePath;
            UploadSlotsBehavior = Preferences.UploadSlotsBehavior;
            UploadChokingAlgorithm = Preferences.UploadChokingAlgorithm;
            AnnounceToAllTrackers = Preferences.AnnounceToAllTrackers;
            AnnounceToAllTiers = Preferences.AnnounceToAllTiers;
            AnnounceIp = Preferences.AnnounceIp;
            MaxConcurrentHttpAnnounces = Preferences.MaxConcurrentHttpAnnounces;
            StopTrackerTimeout = Preferences.StopTrackerTimeout;
            PeerTurnover = Preferences.PeerTurnover;
            PeerTurnoverCutoff = Preferences.PeerTurnoverCutoff;
            PeerTurnoverInterval = Preferences.PeerTurnoverInterval;
            RequestQueueSize = Preferences.RequestQueueSize;
            DhtBootstrapNodes = Preferences.DhtBootstrapNodes;
            I2pInboundQuantity = Preferences.I2pInboundQuantity;
            I2pOutboundQuantity = Preferences.I2pOutboundQuantity;
            I2pInboundLength = Preferences.I2pInboundLength;
            I2pOutboundLength = Preferences.I2pOutboundLength;

            return true;
        }

        protected async Task ResumeDataStorageTypeChanged(string value)
        {
            ResumeDataStorageType = value;
            UpdatePreferences.ResumeDataStorageType = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task MemoryWorkingSetLimitChanged(int value)
        {
            MemoryWorkingSetLimit = value;
            UpdatePreferences.MemoryWorkingSetLimit = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task CurrentNetworkInterfaceChanged(string value)
        {
            CurrentNetworkInterface = value;
            UpdatePreferences.CurrentNetworkInterface = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);

            NetworkInterfaceAddresses = await ApiClient.GetNetworkInterfaceAddressList(value);
        }

        protected async Task CurrentInterfaceAddressChanged(string value)
        {
            CurrentInterfaceAddress = value;
            UpdatePreferences.CurrentInterfaceAddress = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task SaveResumeDataIntervalChanged(int value)
        {
            SaveResumeDataInterval = value;
            UpdatePreferences.SaveResumeDataInterval = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task TorrentFileSizeLimitChanged(int value)
        {
            TorrentFileSizeLimit = value;
            UpdatePreferences.TorrentFileSizeLimit = value * 1024 * 1024;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task RecheckCompletedTorrentsChanged(bool value)
        {
            RecheckCompletedTorrents = value;
            UpdatePreferences.RecheckCompletedTorrents = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task ConfirmTorrentRecheckChanged(bool value)
        {
            ConfirmTorrentRecheck = value;
            UpdatePreferences.ConfirmTorrentRecheck = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task AppInstanceNameChanged(string value)
        {
            AppInstanceName = value;
            UpdatePreferences.AppInstanceName = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task RefreshIntervalChanged(int value)
        {
            RefreshInterval = value;
            UpdatePreferences.RefreshInterval = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task ResolvePeerCountriesChanged(bool value)
        {
            ResolvePeerCountries = value;
            UpdatePreferences.ResolvePeerCountries = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task ReannounceWhenAddressChangedChanged(bool value)
        {
            ReannounceWhenAddressChanged = value;
            UpdatePreferences.ReannounceWhenAddressChanged = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task BdecodeDepthLimitChanged(int value)
        {
            BdecodeDepthLimit = value;
            UpdatePreferences.BdecodeDepthLimit = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task BdecodeTokenLimitChanged(int value)
        {
            BdecodeTokenLimit = value;
            UpdatePreferences.BdecodeTokenLimit = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task AsyncIoThreadsChanged(int value)
        {
            AsyncIoThreads = value;
            UpdatePreferences.AsyncIoThreads = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task HashingThreadsChanged(int value)
        {
            HashingThreads = value;
            UpdatePreferences.HashingThreads = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task FilePoolSizeChanged(int value)
        {
            FilePoolSize = value;
            UpdatePreferences.FilePoolSize = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task CheckingMemoryUseChanged(int value)
        {
            CheckingMemoryUse = value;
            UpdatePreferences.CheckingMemoryUse = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task DiskCacheChanged(int value)
        {
            DiskCache = value;
            UpdatePreferences.DiskCache = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task DiskCacheTtlChanged(int value)
        {
            DiskCacheTtl = value;
            UpdatePreferences.DiskCacheTtl = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task DiskQueueSizeChanged(int value)
        {
            DiskQueueSize = value;
            UpdatePreferences.DiskQueueSize = value * 1024;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task DiskIoTypeChanged(int value)
        {
            DiskIoType = value;
            UpdatePreferences.DiskIoType = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task DiskIoReadModeChanged(int value)
        {
            DiskIoReadMode = value;
            UpdatePreferences.DiskIoReadMode = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task DiskIoWriteModeChanged(int value)
        {
            DiskIoWriteMode = value;
            UpdatePreferences.DiskIoWriteMode = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task EnableCoalesceReadWriteChanged(bool value)
        {
            EnableCoalesceReadWrite = value;
            UpdatePreferences.EnableCoalesceReadWrite = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task EnablePieceExtentAffinityChanged(bool value)
        {
            EnablePieceExtentAffinity = value;
            UpdatePreferences.EnablePieceExtentAffinity = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task EnableUploadSuggestionsChanged(bool value)
        {
            EnableUploadSuggestions = value;
            UpdatePreferences.EnableUploadSuggestions = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task SendBufferWatermarkChanged(int value)
        {
            SendBufferWatermark = value;
            UpdatePreferences.SendBufferWatermark = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task SendBufferLowWatermarkChanged(int value)
        {
            SendBufferLowWatermark = value;
            UpdatePreferences.SendBufferLowWatermark = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task SendBufferWatermarkFactorChanged(int value)
        {
            SendBufferWatermarkFactor = value;
            UpdatePreferences.SendBufferWatermarkFactor = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task ConnectionSpeedChanged(int value)
        {
            ConnectionSpeed = value;
            UpdatePreferences.ConnectionSpeed = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task SocketSendBufferSizeChanged(int value)
        {
            SocketSendBufferSize = value;
            UpdatePreferences.SocketSendBufferSize = value * 1024;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task SocketReceiveBufferSizeChanged(int value)
        {
            SocketReceiveBufferSize = value;
            UpdatePreferences.SocketReceiveBufferSize = value * 1024;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task SocketBacklogSizeChanged(int value)
        {
            SocketBacklogSize = value;
            UpdatePreferences.SocketBacklogSize = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task OutgoingPortsMinChanged(int value)
        {
            OutgoingPortsMin = value;
            UpdatePreferences.OutgoingPortsMin = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task OutgoingPortsMaxChanged(int value)
        {
            OutgoingPortsMax = value;
            UpdatePreferences.OutgoingPortsMax = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task UpnpLeaseDurationChanged(int value)
        {
            UpnpLeaseDuration = value;
            UpdatePreferences.UpnpLeaseDuration = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task PeerTosChanged(int value)
        {
            PeerTos = value;
            UpdatePreferences.PeerTos = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task UtpTcpMixedModeChanged(int value)
        {
            UtpTcpMixedMode = value;
            UpdatePreferences.UtpTcpMixedMode = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task IdnSupportEnabledChanged(bool value)
        {
            IdnSupportEnabled = value;
            UpdatePreferences.IdnSupportEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task EnableMultiConnectionsFromSameIpChanged(bool value)
        {
            EnableMultiConnectionsFromSameIp = value;
            UpdatePreferences.EnableMultiConnectionsFromSameIp = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task ValidateHttpsTrackerCertificateChanged(bool value)
        {
            ValidateHttpsTrackerCertificate = value;
            UpdatePreferences.ValidateHttpsTrackerCertificate = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task SsrfMitigationChanged(bool value)
        {
            SsrfMitigation = value;
            UpdatePreferences.SsrfMitigation = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task BlockPeersOnPrivilegedPortsChanged(bool value)
        {
            BlockPeersOnPrivilegedPorts = value;
            UpdatePreferences.BlockPeersOnPrivilegedPorts = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task EnableEmbeddedTrackerChanged(bool value)
        {
            EnableEmbeddedTracker = value;
            UpdatePreferences.EnableEmbeddedTracker = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task EmbeddedTrackerPortChanged(int value)
        {
            EmbeddedTrackerPort = value;
            UpdatePreferences.EmbeddedTrackerPort = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task EmbeddedTrackerPortForwardingChanged(bool value)
        {
            EmbeddedTrackerPortForwarding = value;
            UpdatePreferences.EmbeddedTrackerPortForwarding = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task MarkOfTheWebChanged(bool value)
        {
            MarkOfTheWeb = value;
            UpdatePreferences.MarkOfTheWeb = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task PythonExecutablePathChanged(string value)
        {
            PythonExecutablePath = value;
            UpdatePreferences.PythonExecutablePath = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task UploadSlotsBehaviorChanged(int value)
        {
            UploadSlotsBehavior = value;
            UpdatePreferences.UploadSlotsBehavior = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task UploadChokingAlgorithmChanged(int value)
        {
            UploadChokingAlgorithm = value;
            UpdatePreferences.UploadChokingAlgorithm = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task AnnounceToAllTrackersChanged(bool value)
        {
            AnnounceToAllTrackers = value;
            UpdatePreferences.AnnounceToAllTrackers = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task AnnounceToAllTiersChanged(bool value)
        {
            AnnounceToAllTiers = value;
            UpdatePreferences.AnnounceToAllTiers = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task AnnounceIpChanged(string value)
        {
            AnnounceIp = value;
            UpdatePreferences.AnnounceIp = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task MaxConcurrentHttpAnnouncesChanged(int value)
        {
            MaxConcurrentHttpAnnounces = value;
            UpdatePreferences.MaxConcurrentHttpAnnounces = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task StopTrackerTimeoutChanged(int value)
        {
            StopTrackerTimeout = value;
            UpdatePreferences.StopTrackerTimeout = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task PeerTurnoverChanged(int value)
        {
            PeerTurnover = value;
            UpdatePreferences.PeerTurnover = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task PeerTurnoverCutoffChanged(int value)
        {
            PeerTurnoverCutoff = value;
            UpdatePreferences.PeerTurnoverCutoff = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task PeerTurnoverIntervalChanged(int value)
        {
            PeerTurnoverInterval = value;
            UpdatePreferences.PeerTurnoverInterval = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task RequestQueueSizeChanged(int value)
        {
            RequestQueueSize = value;
            UpdatePreferences.RequestQueueSize = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task DhtBootstrapNodesChanged(string value)
        {
            DhtBootstrapNodes = value;
            UpdatePreferences.DhtBootstrapNodes = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task I2pInboundQuantityChanged(int value)
        {
            I2pInboundQuantity = value;
            UpdatePreferences.I2pInboundQuantity = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task I2pOutboundQuantityChanged(int value)
        {
            I2pOutboundQuantity = value;
            UpdatePreferences.I2pOutboundQuantity = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task I2pInboundLengthChanged(int value)
        {
            I2pInboundLength = value;
            UpdatePreferences.I2pInboundLength = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task I2pOutboundLengthChanged(int value)
        {
            I2pOutboundLength = value;
            UpdatePreferences.I2pOutboundLength = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }
    }
}