# Khaos.Flow – User Guide

This package provides the default implementation for building and executing outcome-driven workflows in .NET.

## Installation

```bash
dotnet add package KhaosCode.Flow
```

## Overview

Khaos.Flow implements the interfaces from `KhaosCode.Flow.Abstractions`, providing:
- `FlowExecutor<TContext>` for executing flows
- `FlowBuilder<TContext>` for constructing flows fluently
- `PipelineFlowStep` adapter for running pipelines within flows

## Quick Start

```csharp
using Khaos.Flow;
using Khaos.Flow.Abstractions;

// 1. Define your context
public class OrderContext : IFlowContext
{
    public IServiceProvider Services { get; }
    public Order Order { get; set; }
    public bool PaymentSucceeded { get; set; }
    
    public OrderContext(IServiceProvider services) => Services = services;
}

// 2. Create steps
public class ValidateStep : IFlowStep<OrderContext>
{
    public string Name => "Validate";
    
    public ValueTask<FlowOutcome> ExecuteAsync(OrderContext ctx, CancellationToken ct)
    {
        if (ctx.Order.Items.Count == 0)
            return ValueTask.FromResult(FlowOutcome.Failure);
        return ValueTask.FromResult(FlowOutcome.Success);
    }
}

// 3. Build the flow
var builder = new FlowBuilder<OrderContext>();
var flow = builder
    .AddStep(new ValidateStep())
        .OnOutcome(FlowOutcome.Success, "ProcessPayment")
        .OnOutcome(FlowOutcome.Failure, "HandleError")
    .AddStep(new ProcessPaymentStep())
        .OnOutcome(FlowOutcome.Success, "Fulfill")
        .OnOutcome(FlowOutcome.Custom("Declined"), "HandleDeclined")
    .AddStep(new FulfillStep())
    .AddStep(new HandleErrorStep())
    .Build();

// 4. Execute
var executor = new FlowExecutor<OrderContext>();
var context = new OrderContext(services) { Order = order };
var outcome = await executor.ExecuteAsync(flow, context, cancellationToken);
```

## Building Flows

### FlowBuilder&lt;TContext&gt;

The fluent API for constructing flows:

```csharp
var builder = new FlowBuilder<OrderContext>();

// Add step instances
builder.AddStep(new MyStep());

// Add inline steps
builder.AddStep("LogStart", async (ctx, ct) =>
{
    Console.WriteLine($"Processing {ctx.Order.Id}");
    return FlowOutcome.Success;
});
```

### Configuring Transitions

Use `OnOutcome` to define where to go next:

```csharp
builder
    .AddStep(new CheckInventoryStep())
        .OnOutcome(FlowOutcome.Success, "Ship")
        .OnOutcome(FlowOutcome.Custom("PartialStock"), "PartialShip")
        .OnOutcome(FlowOutcome.Custom("OutOfStock"), "Backorder")
        .OnOutcome(FlowOutcome.Failure, "Error");
```

### Terminal Steps

Steps without transitions are terminal—flow ends after them:

```csharp
builder
    .AddStep(new SendConfirmationStep())  // No OnOutcome = terminal
    .Build();
```

## Executing Flows

### FlowExecutor&lt;TContext&gt;

```csharp
var executor = new FlowExecutor<OrderContext>();

var outcome = await executor.ExecuteAsync(flow, context, cancellationToken);

if (outcome == FlowOutcome.Success)
{
    Console.WriteLine("Flow completed successfully");
}
else if (outcome == FlowOutcome.Failure)
{
    Console.WriteLine("Flow failed");
}
else
{
    Console.WriteLine($"Flow ended with: {outcome.Name}");
}
```

## Pipeline Integration

Run a pipeline as a flow step using `PipelineFlowStep`:

```csharp
using Khaos.Flow.Adapters;

// Create a pipeline
var pipeline = Pipeline.Start<OrderInput>()
    .UseStep(new ValidateStep())
    .UseStep(new EnrichStep())
    .Build();

// Wrap it as a flow step
var pipelineStep = new PipelineFlowStep<OrderContext, OrderInput, EnrichedOrder>(
    name: "ProcessOrder",
    pipeline: pipeline,
    inputSelector: ctx => ctx.OrderInput,        // How to get pipeline input
    resultHandler: (ctx, result) => ctx.Enriched = result  // How to store result
);

// Use in flow
builder
    .AddStep(pipelineStep)
        .OnOutcome(FlowOutcome.Success, "Continue")
        .OnOutcome(FlowOutcome.Failure, "HandlePipelineError");
```

### Outcome Mapping

| Pipeline Result | Flow Outcome |
|-----------------|--------------|
| `StepOutcome.Continue(value)` | `FlowOutcome.Success` |
| `StepOutcome.Abort()` | `FlowOutcome.Failure` |

## Dependency Injection

Register with your DI container:

```csharp
services.AddSingleton<IFlowExecutor<OrderContext>, FlowExecutor<OrderContext>>();
services.AddSingleton<IFlowBuilder<OrderContext>, FlowBuilder<OrderContext>>();
```

## Error Handling

Steps can throw exceptions—handle them in your context or wrap steps:

```csharp
public class SafeStep<TContext> : IFlowStep<TContext>
    where TContext : IFlowContext
{
    private readonly IFlowStep<TContext> _inner;
    
    public string Name => _inner.Name;
    
    public async ValueTask<FlowOutcome> ExecuteAsync(TContext ctx, CancellationToken ct)
    {
        try
        {
            return await _inner.ExecuteAsync(ctx, ct);
        }
        catch (Exception ex)
        {
            // Log, store error in context, etc.
            return FlowOutcome.Failure;
        }
    }
}
```

## Best Practices

1. **Name steps clearly** – Names are used for transition routing.
2. **Keep steps focused** – One responsibility per step.
3. **Use custom outcomes** – For complex branching beyond Success/Failure.
4. **Store state in context** – Pass data between steps via context properties.

## Related Packages

- **KhaosCode.Flow.Abstractions** – Core interfaces.
- **KhaosCode.Pipeline** – Pipeline implementation.
