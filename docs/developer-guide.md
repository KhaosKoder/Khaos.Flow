# Khaos.Flow – Developer Guide

This document explains how to extend and maintain the Flow implementation package.

## Solution Layout

- `src/Khaos.Flow`: Production library implementing flow execution.
- `tests/Khaos.Flow.Tests`: xUnit test suite for all components.
- `scripts/`: PowerShell helper scripts for common workflows.
- `docs/`: Markdown documentation bundled inside the NuGet package.

## Key Types

| Type | Description |
|------|-------------|
| `FlowExecutor<TContext>` | Executes flow definitions by traversing steps based on outcomes |
| `FlowBuilder<TContext>` | Fluent API for constructing flows |
| `FlowStepBuilder<TContext>` | Configures outcome-to-step mappings |
| `FlowDefinition<TContext>` | Immutable flow description |
| `FlowStepDefinition<TContext>` | Step with transition mappings |
| `PipelineFlowStep<TContext>` | Adapter to run a Pipeline as a Flow step |

## Architecture

```
FlowBuilder → FlowDefinition → FlowExecutor
                    ↓
            FlowStepDefinition[]
                    ↓
            IFlowStep<TContext> + Outcome Mappings
```

### Execution Flow

1. `FlowExecutor` starts with the first step in the definition.
2. Each step is executed via `IFlowStep.ExecuteAsync()`.
3. The returned `FlowOutcome` is used to find the next step.
4. Execution continues until no next step is found.
5. The final outcome is returned.

## Coding Guidelines

1. **Immutability**
   - `FlowDefinition` and `FlowStepDefinition` are immutable after construction.
   - `FlowBuilder` creates new instances; it doesn't modify existing flows.

2. **Error Handling**
   - Steps throwing exceptions should be caught and mapped to `FlowOutcome.Failure`.
   - Consider adding `OnError` transition support.

3. **Pipeline Adapter**
   - `PipelineFlowStep` wraps a pipeline to run as a flow step.
   - Maps `StepOutcome.Continue` → `FlowOutcome.Success`.
   - Maps `StepOutcome.Abort` → `FlowOutcome.Failure`.

4. **Context Lifecycle**
   - Context is passed through all steps.
   - Steps can modify context to pass data downstream.
   - Use `context.Services` for dependency resolution.

## Testing

- Run tests: `pwsh ./scripts/Test.ps1`
- Test coverage should include:
  - Linear flows (step → step → step)
  - Branching flows (outcome-based routing)
  - Error scenarios
  - Pipeline adapter behavior

## Build & Packaging

- `pwsh ./scripts/Build.ps1`: Restore + build in Release.
- `pwsh ./scripts/Clean.ps1`: Remove TestResults, artifacts.
- `pwsh ./scripts/Pack.ps1`: Create NuGet package.
- Uses **MinVer** with prefix `Khaos.Flow/v`.

## Dependencies

- **KhaosCode.Flow.Abstractions**: Core interfaces.
- **KhaosCode.Pipeline.Abstractions**: For pipeline adapter.

## Versioning

- Follow the abstractions package version where possible.
- See `docs/versioning-guide.md` for tagging workflow.

## Extending

### Adding New Step Types

Create classes implementing `IFlowStep<TContext>`:

```csharp
public class MyCustomStep<TContext> : IFlowStep<TContext>
    where TContext : IFlowContext
{
    public string Name => "MyCustomStep";
    
    public ValueTask<FlowOutcome> ExecuteAsync(TContext context, CancellationToken ct)
    {
        // Custom logic here
        return ValueTask.FromResult(FlowOutcome.Success);
    }
}
```

### Adding New Adapters

Follow the pattern of `PipelineFlowStep`:
1. Implement `IFlowStep<TContext>`.
2. Wrap the external component.
3. Map results to `FlowOutcome`.
