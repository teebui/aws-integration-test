using Amazon.DynamoDBv2;
using Testcontainers.LocalStack;

namespace DynamoDbIntegrationTests;

public class DynamoDbFixture : IAsyncLifetime
{
    private readonly LocalStackContainer _localStackContainer = new LocalStackBuilder()
        .WithImage("localstack/localstack:3.0")
        .Build();

    public IAmazonDynamoDB DynamoDbClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _localStackContainer.StartAsync();

        var config = new AmazonDynamoDBConfig
        {
            ServiceURL = _localStackContainer.GetConnectionString(),
            UseHttp = true,
            AuthenticationRegion = "us-east-1"
        };

        var credentials = new Amazon.Runtime.BasicAWSCredentials("accessKey", "secretKey");

        DynamoDbClient = new AmazonDynamoDBClient(credentials, config);
    }

    public async Task DisposeAsync()
    {
        DynamoDbClient?.Dispose();
        await _localStackContainer.DisposeAsync();
    }
}
