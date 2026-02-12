using Amazon.IdentityManagement;
using Amazon.Lambda;
using Amazon.StepFunctions;
using Testcontainers.LocalStack;

namespace StepFunctionsIntegrationTests;

public class StepFunctionsFixture : IAsyncLifetime
{
    private readonly LocalStackContainer _localStackContainer = new LocalStackBuilder()
        .WithImage("localstack/localstack:3.0")
        .Build();

    public IAmazonStepFunctions StepFunctionsClient { get; private set; } = null!;
    public IAmazonLambda LambdaClient { get; private set; } = null!;
    public IAmazonIdentityManagementService IamClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _localStackContainer.StartAsync();

        var connectionString = _localStackContainer.GetConnectionString();
        var credentials = new Amazon.Runtime.BasicAWSCredentials("accessKey", "secretKey");
        var region = "us-east-1";

        var stepFunctionsConfig = new AmazonStepFunctionsConfig
        {
            ServiceURL = connectionString,
            UseHttp = true,
            AuthenticationRegion = region
        };
        StepFunctionsClient = new AmazonStepFunctionsClient(credentials, stepFunctionsConfig);

        var lambdaConfig = new AmazonLambdaConfig
        {
            ServiceURL = connectionString,
            UseHttp = true,
            AuthenticationRegion = region
        };
        LambdaClient = new AmazonLambdaClient(credentials, lambdaConfig);

        var iamConfig = new AmazonIdentityManagementServiceConfig
        {
            ServiceURL = connectionString,
            UseHttp = true,
            AuthenticationRegion = region
        };
        IamClient = new AmazonIdentityManagementServiceClient(credentials, iamConfig);
    }

    public async Task DisposeAsync()
    {
        StepFunctionsClient?.Dispose();
        LambdaClient?.Dispose();
        IamClient?.Dispose();
        await _localStackContainer.DisposeAsync();
    }
}
