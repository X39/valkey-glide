using Aspire.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Valkey.Glide.InterOp;
using Valkey.Glide.InterOp.Native;

namespace Valkey.Glide.IntegrationTests.Fixtures;

public sealed class ValkeySingleAspireFixture : IAsyncLifetime
{
    private DistributedApplication? _distributedApplication;

    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Valkey_Glide_AppHost>();
        _distributedApplication = await appHost.BuildAsync();
        await _distributedApplication.StartAsync();
        try
        {
            await LoadSingleMode();
            await LoadClusterMode();
        }
        catch
        {
            await _distributedApplication.StopAsync();
            await _distributedApplication.DisposeAsync();
            _distributedApplication = null;
            throw;
        }

        if (NativeLoggingHarness.Instance is not null)
            _ = new StdOutLoggingHarness();
    }

    private async Task LoadSingleMode()
    {
        var uri = await GetValkeyConnectionUri("valkey-single");
        SingleConnectionRequest = new InterOp.ConnectionRequest([new Node(uri.Host, (ushort) uri.Port)])
            { TlsMode = uri.Scheme == "https" ? InterOp.ETlsMode.SecureTls : null };
    }

    private async Task LoadClusterMode()
    {
        var valkeyNodeMasterUri = await GetValkeyConnectionUri("valkey-node-master");
        var valkeyNodeAUri = await GetValkeyConnectionUri("valkey-node-a");
        var valkeyNodeBUri = await GetValkeyConnectionUri("valkey-node-b");
        var valkeyNodeCUri = await GetValkeyConnectionUri("valkey-node-c");
        ClusterConnectionRequest = new InterOp.ConnectionRequest(
            [
                new Node(valkeyNodeMasterUri.Host, (ushort) valkeyNodeMasterUri.Port),
                new Node(valkeyNodeAUri.Host, (ushort) valkeyNodeAUri.Port),
                new Node(valkeyNodeBUri.Host, (ushort) valkeyNodeBUri.Port),
                new Node(valkeyNodeCUri.Host, (ushort) valkeyNodeCUri.Port),
            ]
        ) { TlsMode = valkeyNodeMasterUri.Scheme == "https" ? InterOp.ETlsMode.SecureTls : null, ClusterMode = true };
    }

    private InterOp.ConnectionRequest? _connectionRequest;

    public InterOp.ConnectionRequest SingleConnectionRequest
    {
        get
            => _connectionRequest
               ?? throw new InvalidOperationException("Not initialized. Call InitializeAsync() first.");
        private set => _connectionRequest = value;
    }

    private InterOp.ConnectionRequest? _clusterConnectionRequest;

    public InterOp.ConnectionRequest ClusterConnectionRequest
    {
        get
            => _clusterConnectionRequest
               ?? throw new InvalidOperationException("Not initialized. Call InitializeAsync() first.");
        private set => _clusterConnectionRequest = value;
    }

    public async Task DisposeAsync()
    {
        if (_distributedApplication is not null)
        {
            await _distributedApplication.StopAsync();
            await _distributedApplication.DisposeAsync();
            _distributedApplication = null;
        }
    }

    private async Task<Uri> GetValkeyConnectionUri(string resourceName)
    {
        if (_distributedApplication is null)
            throw new InvalidOperationException("Not initialized. Call InitializeAsync() first.");
        var resourceEvent = await _distributedApplication.ResourceNotifications.WaitForResourceHealthyAsync(
            resourceName,
            WaitBehavior.StopOnResourceUnavailable
        );
        if (resourceEvent.Snapshot.HealthStatus is not HealthStatus.Healthy)
            throw new Exception("Cache is not healthy, aspire initialization failed.");
        var url = resourceEvent.Snapshot.Urls.FirstOrDefault();
        if (url is null)
            throw new Exception("Cache has no URL, aspire initialization failed.");
        var uri = new Uri(url.Url);
        return uri;
    }
}
