using DotNet.Testcontainers.Builders;
using OpenSearch.Client;
using Testcontainers.OpenSearch;

namespace OpenSearchIntegrationTests;

public class OpenSearchFixture : IAsyncLifetime
{
    private OpenSearchContainer? _container;

    public OpenSearchClient Client { get; private set; } = null!;
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _container = new OpenSearchBuilder("opensearchproject/opensearch:2.11.0")
            .WithEnvironment("discovery.type", "single-node")
            .WithEnvironment("OPENSEARCH_INITIAL_ADMIN_PASSWORD", "MyStrongPassword123!")
            .WithEnvironment("plugins.security.disabled", "true")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(9200)))
            .Build();

        await _container.StartAsync();

        ConnectionString = _container.GetConnectionString();

        var connectionString = ConnectionString.Replace("https://", "http://");
        var settings = new ConnectionSettings(new Uri(connectionString))
            .DisableDirectStreaming()
            .ServerCertificateValidationCallback((o, certificate, chain, errors) => true)
            .ThrowExceptions();

        Client = new OpenSearchClient(settings);

        await Task.Delay(2000);
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }
}
