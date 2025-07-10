using Microsoft.Extensions.DependencyInjection;

namespace Valkey.Glide.AppHost;

public static class DistributedApplicationBuilderExtensions
{
    public static IResourceBuilder<ValkeyResource> AddValkeySingle(
        this IDistributedApplicationBuilder builder,
        string name,
        int? port = null
    )
    {
        var resource = new ValkeyResource(name);
        return builder.AddResource(resource)
            .WithEndpoint(port: port, targetPort: 6379, name: Constants.PrimaryEndpointName)
            .WithEndpoint(targetPort: 8080, name: Constants.HttpEndpointName, scheme: Constants.HttpEndpointScheme)
            .WithImage("valkey/valkey") // Required due to a bug in this aspire version
            .WithDockerfile("ValkeyWith8080HttpHealthCheck")
            .WithHttpHealthCheck("/health");
    }

    public static IResourceBuilder<ValkeyClusterResource> AddValkeyCluster(
        this IDistributedApplicationBuilder builder,
        string name,
        int? port = null
    )
    {
        var resource = new ValkeyClusterResource(name);
        return builder.AddResource(resource)
            .WithEndpoint(port: port, targetPort: 6379, name: Constants.PrimaryEndpointName)
            .WithEndpoint(targetPort: 8080, name: Constants.HttpEndpointName, scheme: Constants.HttpEndpointScheme)
            .WithImage("valkey/valkey") // Required due to a bug in this aspire version
            .WithDockerfile("ValkeyWith8080HttpHealthCheck")
            .WithEnvironment("VALKEY_EXTRA_FLAGS", "--cluster-enabled yes --port 6380")
            .WithHttpHealthCheck("/health");
    }

    public static IResourceBuilder<ValkeyClusterResource> AddMasterReference(
        this IResourceBuilder<ValkeyClusterResource> builder,
        IResourceBuilder<ValkeyClusterResource> reference
    )
    {
        return builder.WithReference(reference)
            .WithEnvironment(async context =>
                {
                    var portResource = reference.Resource.PrimaryEndpoint.Property(EndpointProperty.Port);
                    var port = await portResource.GetValueAsync(default);
                    context.EnvironmentVariables["VALKEY_EXTRA_FLAGS"] = $"--cluster-enabled yes --port {port}";
                }
            );
    }
}
