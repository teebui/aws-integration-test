using Amazon.Athena;
using Amazon.S3;
using Amazon.S3.Model;
using Testcontainers.LocalStack;

namespace AthenaIntegrationTests;

public class AthenaFixture : IAsyncLifetime
{
    private readonly LocalStackContainer _localStackContainer = new LocalStackBuilder()
        .WithImage("localstack/localstack:3.0")
        .Build();

    public IAmazonAthena AthenaClient { get; private set; } = null!;
    public IAmazonS3 S3Client { get; private set; } = null!;
    public string OutputBucket { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _localStackContainer.StartAsync();

        var config = new AmazonAthenaConfig
        {
            ServiceURL = _localStackContainer.GetConnectionString(),
            UseHttp = true,
            AuthenticationRegion = "us-east-1"
        };
        
        var s3Config = new AmazonS3Config
        {
            ServiceURL = _localStackContainer.GetConnectionString(),
            UseHttp = true,
            AuthenticationRegion = "us-east-1",
            ForcePathStyle = true
        };

        var credentials = new Amazon.Runtime.BasicAWSCredentials("accessKey", "secretKey");

        AthenaClient = new AmazonAthenaClient(credentials, config);
        S3Client = new AmazonS3Client(credentials, s3Config);

        OutputBucket = $"athena-test-output-{Guid.NewGuid()}";
        
        try 
        {
            await S3Client.PutBucketAsync(OutputBucket);
        }
        catch(AmazonS3Exception ex) when (ex.ErrorCode == "BucketAlreadyOwnedByYou")
        {
            // Ignore if bucket already exists
        }
    }

    public async Task DisposeAsync()
    {
        if (S3Client != null && !string.IsNullOrEmpty(OutputBucket))
        {
            try
            {
                // Empty bucket before deleting
                var objects = await S3Client.ListObjectsV2Async(new ListObjectsV2Request { BucketName = OutputBucket });
                foreach (var obj in objects.S3Objects)
                {
                    await S3Client.DeleteObjectAsync(OutputBucket, obj.Key);
                }
                
                await S3Client.DeleteBucketAsync(OutputBucket);
            }
            catch
            {
                // Best effort cleanup
            }
        }
        AthenaClient?.Dispose();
        S3Client?.Dispose();
        await _localStackContainer.DisposeAsync();
    }
}
