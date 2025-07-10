namespace Valkey.Glide.AppHost;

public class ValkeyResource(string name) : ContainerResource(name), IResourceWithConnectionString
{
    private EndpointReference? _primaryEndpoint;

    /// <summary>
    /// Gets the primary endpoint for the Valkey server.
    /// </summary>
    public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new(this, Constants.PrimaryEndpointName);

    /// <summary>
    /// Gets the connection string expression for the Valkey server.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression
        => ReferenceExpression.Create(
            $"HOST={PrimaryEndpoint.Property(EndpointProperty.Host)}:{PrimaryEndpoint.Property(EndpointProperty.Port)};CLUSTERED=FALSE;PROTOCOL=RESP3;"
        );
}
