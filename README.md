# Khaos.Flow

Implementation of state-machine workflow orchestration with branching and transitions.

## Overview

This package provides the concrete implementation for executing flows - state-machine workflows where each step returns an outcome that determines the next step to execute.

## Key Types

| Type | Description |
|------|-------------|
| `FlowExecutor<TContext>` | Executes flow definitions |
| `FlowBuilder<TContext>` | Fluent API for building flows |
| `FlowDefinition<TContext>` | Immutable flow description |
| `FlowStepDefinition` | Step with transition mappings |
| `PipelineFlowStep<TContext>` | Adapter to run a Pipeline as a Flow step |

## Usage

```csharp
// Build a flow
var flow = Flow.Create<StartupContext>("MyFlow")
    .BeginWith<ValidateDatabaseStep>()
    .On(FlowOutcome.Success).Then<LoadConfigStep>()
    .On(FlowOutcome.Failure).GoTo<LogErrorStep>()
    .Build();

// Execute the flow
var executor = new FlowExecutor<StartupContext>();
var outcome = await executor.ExecuteAsync(flow, context, cancellationToken);
```

## Pipeline Integration

Flows can execute pipelines as steps using the `PipelineFlowStep` adapter:

```csharp
// Run a pipeline within a flow step
var flow = Flow.Create<MyContext>("ProcessingFlow")
    .BeginWith<PrepareStep>()
    .Then<PipelineFlowStep<MyContext, InputRecord, OutputRecord>>()
    .Then<FinalizeStep>()
    .Build();
```

## Related Packages

- `KhaosCode.Flow.Abstractions` - Interfaces this package implements
- `KhaosCode.Pipeline.Abstractions` - Pipeline abstractions
- `KhaosCode.Pipeline` - Pipeline implementation

## License

MIT License - see [LICENSE.md](LICENSE.md)
