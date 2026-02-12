using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Shouldly;
using Xunit.Abstractions;

namespace StepFunctionsIntegrationTests;

public class StepFunctionsTests : IClassFixture<StepFunctionsFixture>
{
    private readonly StepFunctionsFixture _fixture;
    private readonly ITestOutputHelper _output;

    public StepFunctionsTests(StepFunctionsFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task CanCreateAndListStateMachines()
    {
        var stateMachineName = $"test-state-machine-{Guid.NewGuid()}";
        var roleArn = "arn:aws:iam::000000000000:role/service-role/Role"; // Dummy role for LocalStack
        var definition = @"{
          ""Comment"": ""A Hello World example of the Amazon States Language using an AWS Lambda Function"",
          ""StartAt"": ""HelloWorld"",
          ""States"": {
            ""HelloWorld"": {
              ""Type"": ""Pass"",
              ""Result"": ""Hello World!"",
              ""End"": true
            }
          }
        }";

        try
        {
            var createRequest = new CreateStateMachineRequest
            {
                Name = stateMachineName,
                Definition = definition,
                RoleArn = roleArn
            };

            var createResponse = await _fixture.StepFunctionsClient.CreateStateMachineAsync(createRequest);
            createResponse.StateMachineArn.ShouldNotBeNullOrEmpty();

            var listRequest = new ListStateMachinesRequest();
            var listResponse = await _fixture.StepFunctionsClient.ListStateMachinesAsync(listRequest);

            listResponse.StateMachines.ShouldContain(sm => sm.Name == stateMachineName);
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Error in CanCreateAndListStateMachines: {ex}");
            throw;
        }
    }

    [Fact]
    public async Task CanStartExecution()
    {
        var stateMachineName = $"exec-test-{Guid.NewGuid()}";
        var roleArn = "arn:aws:iam::000000000000:role/service-role/Role";
        var definition = @"{
          ""StartAt"": ""Pass"",
          ""States"": {
            ""Pass"": {
              ""Type"": ""Pass"",
              ""Result"": ""Success"",
              ""End"": true
            }
          }
        }";

        try
        {
            var createResponse = await _fixture.StepFunctionsClient.CreateStateMachineAsync(new CreateStateMachineRequest
            {
                Name = stateMachineName,
                Definition = definition,
                RoleArn = roleArn
            });

            var startExecutionRequest = new StartExecutionRequest
            {
                StateMachineArn = createResponse.StateMachineArn,
                Input = "{}"
            };

            var startResponse = await _fixture.StepFunctionsClient.StartExecutionAsync(startExecutionRequest);
            startResponse.ExecutionArn.ShouldNotBeNullOrEmpty();
            
            // Wait for execution to complete
            var executionArn = startResponse.ExecutionArn;
            var isRunning = true;
            while (isRunning)
            {
                var describeResponse = await _fixture.StepFunctionsClient.DescribeExecutionAsync(new DescribeExecutionRequest
                {
                    ExecutionArn = executionArn
                });

                if (describeResponse.Status == ExecutionStatus.SUCCEEDED || 
                    describeResponse.Status == ExecutionStatus.FAILED || 
                    describeResponse.Status == ExecutionStatus.TIMED_OUT ||
                    describeResponse.Status == ExecutionStatus.ABORTED)
                {
                    isRunning = false;
                    describeResponse.Status.ShouldBe(ExecutionStatus.SUCCEEDED);
                }
                else
                {
                    await Task.Delay(1000);
                }
            }
        }
        catch (Exception ex)
        {
             _output.WriteLine($"Error in CanStartExecution: {ex}");
             throw;
        }
    }
}
