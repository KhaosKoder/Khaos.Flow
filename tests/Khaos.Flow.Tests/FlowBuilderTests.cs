using Khaos.Flow.Abstractions;

namespace Khaos.Flow.Tests;

public class FlowBuilderTests
{
    [Fact]
    public void Create_WithValidName_ReturnsBuilder()
    {
        var builder = Flow.Create<TestContext>("TestFlow");
        Assert.NotNull(builder);
    }

    [Fact]
    public void Create_WithNullName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Flow.Create<TestContext>(null!));
    }

    [Fact]
    public void Create_WithWhitespaceName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Flow.Create<TestContext>("   "));
    }

    [Fact]
    public void Build_WithoutStartingStep_ThrowsInvalidOperationException()
    {
        var builder = Flow.Create<TestContext>("TestFlow");
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Build_WithStartingStep_ReturnsFlowDefinition()
    {
        var flow = Flow.Create<TestContext>("TestFlow")
            .BeginWith<SuccessStep>()
            .Build();

        Assert.NotNull(flow);
        Assert.Equal("TestFlow", flow.Name);
        Assert.NotNull(flow.StartStepKey);
        Assert.Single(flow.Steps);
    }

    [Fact]
    public void Build_CalledTwice_ThrowsInvalidOperationException()
    {
        var builder = Flow.Create<TestContext>("TestFlow")
            .BeginWith<SuccessStep>();

        builder.Build();
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void BeginWith_CalledTwice_ThrowsInvalidOperationException()
    {
        var builder = Flow.Create<TestContext>("TestFlow")
            .BeginWith<SuccessStep>()
            .End();

        Assert.Throws<InvalidOperationException>(() => builder.BeginWith<SecondStep>());
    }

    [Fact]
    public void Then_AddsTransitionToNextStep()
    {
        var flow = Flow.Create<TestContext>("TestFlow")
            .BeginWith<SuccessStep>()
            .Then<SecondStep>()
            .Build();

        Assert.Equal(2, flow.Steps.Count);
    }

    [Fact]
    public void On_Then_AddsTransitionForSpecificOutcome()
    {
        var flow = Flow.Create<TestContext>("TestFlow")
            .BeginWith<SuccessStep>()
            .On(FlowOutcome.Failure).GoTo<FailureStep>()
            .Then<SecondStep>()
            .Build();

        Assert.Equal(3, flow.Steps.Count);
    }
}
