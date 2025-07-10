namespace Valkey.Glide.AppHost;

internal static class Constants
{
    /// <summary>
    /// The docker registry to be used.
    /// </summary>
    /// <remarks>
    /// Defaults to:
    /// docker.io
    /// </remarks>
    public const string Registry = "docker.io";

    /// <summary>
    /// The docker image to use.
    /// </summary>
    /// <remarks>
    /// Defaults to:
    /// valkey/valkey
    /// </remarks>
    public const string Image = "valkey/valkey";

    /// <summary>
    /// The tag for the <see cref="Image"/> to use.
    /// </summary>
    /// <remarks>
    /// Defaults to:
    /// 8.1.2
    /// </remarks>
    public const string Tag = "8.1.2";

    /// <summary>
    /// The primary endpoint name of this service.
    /// </summary>
    public const string PrimaryEndpointName = "tcp";

    /// <summary>
    /// The name of the HTTP endpoint for the service.
    /// </summary>
    /// <remarks>
    /// Used to configure the HTTP endpoint of the service, typically associated with port 8080.
    /// </remarks>
    public const string HttpEndpointName = "http";


    /// <summary>
    /// The scheme to be used for the HTTP endpoint of the service.
    /// </summary>
    /// <remarks>
    /// Defaults to:
    /// http
    /// </remarks>
    public const string HttpEndpointScheme = "http";

    public const string ConnectionStringEndpointName = "ConnectionString";
}
