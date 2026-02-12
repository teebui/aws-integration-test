using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Shouldly;
using Xunit.Abstractions;

namespace DynamoDbIntegrationTests;

public class DynamoDbTests : IClassFixture<DynamoDbFixture>
{
    private readonly DynamoDbFixture _fixture;
    private readonly ITestOutputHelper _output;

    public DynamoDbTests(DynamoDbFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task CanCreateAndQueryTable()
    {
        var tableName = $"TestTable-{Guid.NewGuid()}";

        // Create table
        var createRequest = new CreateTableRequest
        {
            TableName = tableName,
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new AttributeDefinition("Id", ScalarAttributeType.S)
            },
            KeySchema = new List<KeySchemaElement>
            {
                new KeySchemaElement("Id", KeyType.HASH)
            },
            ProvisionedThroughput = new ProvisionedThroughput(5, 5)
        };

        await _fixture.DynamoDbClient.CreateTableAsync(createRequest);

        // Wait for table to be active
        var isTableActive = false;
        while (!isTableActive)
        {
            var describeResponse = await _fixture.DynamoDbClient.DescribeTableAsync(tableName);
            if (describeResponse.Table.TableStatus == TableStatus.ACTIVE)
            {
                isTableActive = true;
            }
            else
            {
                await Task.Delay(500);
            }
        }

        // Put item
        await _fixture.DynamoDbClient.PutItemAsync(new PutItemRequest
        {
            TableName = tableName,
            Item = new Dictionary<string, AttributeValue>
            {
                { "Id", new AttributeValue { S = "1" } },
                { "Data", new AttributeValue { S = "Hello DynamoDB" } }
            }
        });

        // Get item
        var getResponse = await _fixture.DynamoDbClient.GetItemAsync(new GetItemRequest
        {
            TableName = tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                { "Id", new AttributeValue { S = "1" } }
            }
        });

        getResponse.Item.ShouldNotBeNull();
        getResponse.Item["Data"].S.ShouldBe("Hello DynamoDB");

        // Cleanup
        await _fixture.DynamoDbClient.DeleteTableAsync(tableName);
    }
}
