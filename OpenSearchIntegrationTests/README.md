# OpenSearch Integration Tests

This project demonstrates .NET integration testing with OpenSearch using Testcontainers.

## Overview

This project includes:
- **OpenSearchFixture**: A test fixture that manages the OpenSearch container lifecycle
- **OpenSearchTests**: Comprehensive integration tests demonstrating CRUD operations and search functionality
- **Testcontainers**: Docker-based testing infrastructure for running OpenSearch in a container

## Prerequisites

- .NET 10.0 or later
- Docker (Docker Desktop, OrbStack, or similar)
- NuGet packages:
  - `xunit`
  - `Testcontainers` (v4.10.0)
  - `Testcontainers.OpenSearch` (v4.10.0)
  - `OpenSearch.Client` (v1.8.0)
  - `Shouldly` (v4.3.0)

## Project Structure

```
OpenSearchIntegrationTests/
├── OpenSearchFixture.cs       # Test fixture managing OpenSearch container
├── OpenSearchTests.cs         # Integration tests
└── OpenSearchIntegrationTests.csproj
```

## Test Fixture Features

The `OpenSearchFixture` class:
- Automatically starts an OpenSearch 2.11.0 container before tests
- Configures the container with security disabled for easier testing
- Provides a pre-configured `OpenSearchClient` instance
- Automatically cleans up the container after tests complete
- Uses the XUnit `IClassFixture<T>` pattern for efficient resource sharing

## Available Tests

1. **CanConnectToOpenSearch**: Verifies basic connectivity to the OpenSearch instance
2. **CanCreateAndDeleteIndex**: Tests index creation and deletion operations
3. **CanIndexAndRetrieveDocument**: Tests document indexing and retrieval
4. **CanSearchDocuments**: Tests search functionality with multiple documents

## Running the Tests

To run all tests:

```bash
dotnet test
```

To run tests with detailed output:

```bash
dotnet test --logger "console;verbosity=normal"
```

To run a specific test:

```bash
dotnet test --filter "FullyQualifiedName~OpenSearchTests.CanConnectToOpenSearch"
```

## Configuration Details

### OpenSearch Container Configuration

The fixture configures the OpenSearch container with:
- Single-node discovery mode
- Security plugins disabled for simplicity
- HTTP protocol (not HTTPS)
- Port 9200 exposed for API access
- Wait strategy to ensure container is ready before tests run

### Client Configuration

The OpenSearch client is configured with:
- HTTP connection (security disabled)
- Direct streaming disabled for debugging
- Certificate validation disabled for testing
- ThrowExceptions enabled for better error messages

## Test Best Practices

Each test in this project:
- Creates uniquely named indices to avoid conflicts
- Includes proper cleanup in `finally` blocks
- Uses `Refresh.WaitFor` to ensure documents are searchable
- Uses `Shouldly` for fluent assertions
- Tests a single concern per test method

## Example Test Document

```csharp
public class TestDocument
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

## Extending the Tests

To add new tests:

1. Add a new test method to `OpenSearchTests`
2. Use the `_fixture.Client` to interact with OpenSearch
3. Create unique index names to avoid conflicts
4. Include proper cleanup in `finally` blocks
5. Use `Shouldly` assertions for clear error messages

Example:

```csharp
[Fact]
public async Task YourNewTest()
{
    // Arrange
    var indexName = $"test-index-{Guid.NewGuid()}";
    
    try
    {
        // Act
        // ... your test code
        
        // Assert
        response.IsValid.ShouldBeTrue();
        result.ShouldBe("expected");
    }
    finally
    {
        // Cleanup
        await _fixture.Client.Indices.DeleteAsync(indexName);
    }
}
```

## Troubleshooting

### Docker Connection Issues

If tests fail to start the container, ensure:
- Docker is running
- Your user has permission to access Docker
- The Docker socket is accessible

### SSL/TLS Errors

If you encounter SSL errors:
- Verify that the connection string uses `http://` not `https://`
- Check that `plugins.security.disabled` is set to `true`
- Ensure `ServerCertificateValidationCallback` is configured

### Port Conflicts

If port 9200 is already in use:
- Stop any existing OpenSearch/Elasticsearch instances
- Testcontainers will automatically assign available ports

## Resources

- [OpenSearch Documentation](https://opensearch.org/docs/latest/)
- [Testcontainers for .NET](https://dotnet.testcontainers.org/)
- [XUnit Documentation](https://xunit.net/)
- [OpenSearch .NET Client](https://github.com/opensearch-project/opensearch-net)
