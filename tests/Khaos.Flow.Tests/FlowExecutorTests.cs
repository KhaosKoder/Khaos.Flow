using Khaos.Flow.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Khaos.Flow.Tests;

public class FlowExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_SingleStep_ReturnsSuccess()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<SuccessStep>();
        var provider = services.BuildServiceProvider();
        var context = new TestContext(provider);

        var flow = Flow.Create<TestContext>("TestFlow")
            .BeginWith<SuccessStep>()
            .Build();

        var executor = new FlowExecutor<TestContext>();

        // Act
        var outcome = await executor.ExecuteAsync(flow, context, CancellationToken.None);

        // Assert
        Assert.Equal(FlowOutcome.Success, outcome);
    }

    [Fact]
    public async Task ExecuteAsync_WithTransition_FollowsSuccessPath()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<SuccessStep>();
        services.AddTransient<SecondStep>();
        var provider = services.BuildServiceProvider();
        var context = new TestContext(provider);

        var flow = Flow.Create<TestContext>("TestFlow")
            .BeginWith<SuccessStep>()
            .Then<SecondStep>()
            .Build();

        var executor = new FlowExecutor<TestContext>();

        // Act
        var outcome = await executor.ExecuteAsync(flow, context, CancellationToken.None);

        // Assert
        Assert.Equal(FlowOutcome.Success, outcome);
        Assert.Equal(2, context.StepsExecuted);
    }

    [Fact]
    public async Task ExecuteAsync_WithFailure_FollowsFailurePath()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<FailureStep>();
        services.AddTransient<RecoveryStep>();
        var provider = services.BuildServiceProvider();
        var context = new TestContext(provider);

        var flow = Flow.Create<TestContext>("TestFlow")
            .BeginWith<FailureStep>()
            .On(FlowOutcome.Failure).GoTo<RecoveryStep>()
            .End()
            .Build();

        var executor = new FlowExecutor<TestContext>();

        // Act
        var outcome = await executor.ExecuteAsync(flow, context, CancellationToken.None);

        // Assert
        Assert.Equal(FlowOutcome.Success, outcome);
        Assert.Contains("FailureStep", context.ExecutedStepNames);
        Assert.Contains("RecoveryStep", context.ExecutedStepNames);
    }

    [Fact]
    public async Task ExecuteAsync_WithCustomOutcome_FollowsCustomPath()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<RetryStep>();
        services.AddTransient<SecondStep>();
        var provider = services.BuildServiceProvider();
        var context = new TestContext(provider);

        var retryOutcome = FlowOutcome.Custom("Retry");
        var flow = Flow.Create<TestContext>("TestFlow")
            .BeginWith<RetryStep>()
            .On(retryOutcome).GoTo<SecondStep>()
            .End()
            .Build();

        var executor = new FlowExecutor<TestContext>();

        // Act
        var outcome = await executor.ExecuteAsync(flow, context, CancellationToken.None);

        // Assert
        Assert.Equal(FlowOutcome.Success, outcome);
        Assert.Contains("RetryStep", context.ExecutedStepNames);
        Assert.Contains("SecondStep", context.ExecutedStepNames);
    }
}

// Test helpers
public class TestContext : IFlowContext
{
    public TestContext(IServiceProvider services) => Services = services;
    public IServiceProvider Services { get; }
    public int StepsExecuted { get; set; }
    public List<string> ExecutedStepNames { get; } = new();
}

public class SuccessStep : IFlowStep<TestContext>
{
    public Task<FlowOutcome> ExecuteAsync(TestContext context, CancellationToken ct)
    {
        context.StepsExecuted++;
        context.ExecutedStepNames.Add("SuccessStep");
        return Task.FromResult(FlowOutcome.Success);
    }
}

public class SecondStep : IFlowStep<TestContext>
{
    public Task<FlowOutcome> ExecuteAsync(TestContext context, CancellationToken ct)
    {
        context.StepsExecuted++;
        context.ExecutedStepNames.Add("SecondStep");
        return Task.FromResult(FlowOutcome.Success);
    }
}

public class FailureStep : IFlowStep<TestContext>
{
    public Task<FlowOutcome> ExecuteAsync(TestContext context, CancellationToken ct)
    {
        context.StepsExecuted++;
        context.ExecutedStepNames.Add("FailureStep");
        return Task.FromResult(FlowOutcome.Failure);
    }
}

public class RecoveryStep : IFlowStep<TestContext>
{
    public Task<FlowOutcome> ExecuteAsync(TestContext context, CancellationToken ct)
    {
        context.StepsExecuted++;
        context.ExecutedStepNames.Add("RecoveryStep");
        return Task.FromResult(FlowOutcome.Success);
    }
}

public class RetryStep : IFlowStep<TestContext>
{
    public Task<FlowOutcome> ExecuteAsync(TestContext context, CancellationToken ct)
    {
        context.StepsExecuted++;
        context.ExecutedStepNames.Add("RetryStep");
        return Task.FromResult(FlowOutcome.Custom("Retry"));
    }
}
