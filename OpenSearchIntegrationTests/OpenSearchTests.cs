using OpenSearch.Client;
using Shouldly;

namespace OpenSearchIntegrationTests;

public class OpenSearchTests : IClassFixture<OpenSearchFixture>
{
    private readonly OpenSearchFixture _fixture;
    private const string TestIndexName = "test-index";

    public OpenSearchTests(OpenSearchFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CanConnectToOpenSearch()
    {
        var pingResponse = await _fixture.Client.PingAsync();

        pingResponse.IsValid.ShouldBeTrue("Should be able to ping OpenSearch");
    }

    [Fact]
    public async Task CanCreateAndDeleteIndex()
    {
        var indexName = $"{TestIndexName}-{Guid.NewGuid()}";

        try
        {
            var createResponse = await _fixture.Client.Indices.CreateAsync(indexName);

            createResponse.IsValid.ShouldBeTrue($"Should create index successfully. Error: {createResponse.DebugInformation}");
            createResponse.Acknowledged.ShouldBeTrue("Index creation should be acknowledged");

            var existsResponse = await _fixture.Client.Indices.ExistsAsync(indexName);
            existsResponse.Exists.ShouldBeTrue("Index should exist after creation");
        }
        finally
        {
            var deleteResponse = await _fixture.Client.Indices.DeleteAsync(indexName);
            (deleteResponse.IsValid || deleteResponse.ServerError?.Status == 404)
                .ShouldBeTrue("Should delete index successfully or it should not exist");
        }
    }

    [Fact]
    public async Task CanIndexAndRetrieveDocument()
    {
        var indexName = $"{TestIndexName}-{Guid.NewGuid()}";
        var testDocument = new TestDocument
        {
            Id = "1",
            Title = "Test Document",
            Content = "This is a test document for OpenSearch integration testing",
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await _fixture.Client.Indices.CreateAsync(indexName);

            var indexResponse = await _fixture.Client.IndexAsync(testDocument, idx => idx
                .Index(indexName)
                .Id(testDocument.Id)
                .Refresh(OpenSearch.Net.Refresh.WaitFor));

            indexResponse.IsValid.ShouldBeTrue($"Should index document successfully. Error: {indexResponse.DebugInformation}");
            indexResponse.Result.ToString().ToLower().ShouldBe("created");

            var getResponse = await _fixture.Client.GetAsync<TestDocument>(testDocument.Id, g => g.Index(indexName));

            getResponse.IsValid.ShouldBeTrue("Should retrieve document successfully");
            getResponse.Found.ShouldBeTrue("Document should be found");
            getResponse.Source.ShouldNotBeNull();
            getResponse.Source.Title.ShouldBe(testDocument.Title);
            getResponse.Source.Content.ShouldBe(testDocument.Content);
        }
        finally
        {
            await _fixture.Client.Indices.DeleteAsync(indexName);
        }
    }

    [Fact]
    public async Task CanSearchDocuments()
    {
        var indexName = $"{TestIndexName}-{Guid.NewGuid()}";
        var documents = new[]
        {
            new TestDocument { Id = "1", Title = "First Document", Content = "Content about testing" },
            new TestDocument { Id = "2", Title = "Second Document", Content = "Content about OpenSearch" },
            new TestDocument { Id = "3", Title = "Third Document", Content = "Content about integration" }
        };

        try
        {
            await _fixture.Client.Indices.CreateAsync(indexName);

            foreach (var doc in documents)
            {
                await _fixture.Client.IndexAsync(doc, idx => idx
                    .Index(indexName)
                    .Id(doc.Id));
            }

            await _fixture.Client.Indices.RefreshAsync(indexName);

            var searchResponse = await _fixture.Client.SearchAsync<TestDocument>(s => s
                .Index(indexName)
                .Query(q => q
                    .Match(m => m
                        .Field(f => f.Content)
                        .Query("OpenSearch")
                    )
                )
            );

            searchResponse.IsValid.ShouldBeTrue($"Search should be valid. Error: {searchResponse.DebugInformation}");
            searchResponse.Documents.ShouldNotBeEmpty();
            searchResponse.Documents.ShouldHaveSingleItem().Title.ShouldBe("Second Document");
        }
        finally
        {
            await _fixture.Client.Indices.DeleteAsync(indexName);
        }
    }
}

public class TestDocument
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
