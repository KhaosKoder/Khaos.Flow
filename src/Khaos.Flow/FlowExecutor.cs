using Khaos.Flow.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Khaos.Flow;

/// <summary>
/// Exception thrown when flow execution fails.
/// </summary>
public class FlowExecutionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FlowExecutionException"/> class.
    /// </summary>
    public FlowExecutionException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance with an inner exception.
    /// </summary>
    public FlowExecutionException(string message, Exception innerException) 
        : base(message, innerException) { }
}

/// <summary>
/// Executes flow definitions using their step definitions and transitions.
/// </summary>
/// <typeparam name="TContext">The flow context type.</typeparam>
public sealed class FlowExecutor<TContext> : IFlowExecutor<TContext> 
    where TContext : IFlowContext
{
    /// <inheritdoc />
    public async Task<FlowOutcome> ExecuteAsync(
        IFlowDefinition<TContext> flow,
        TContext context,
        CancellationToken cancellationToken)
    {
        if (flow is null) throw new ArgumentNullException(nameof(flow));
        if (context is null) throw new ArgumentNullException(nameof(context));

        var currentKey = flow.StartStepKey;
        var lastOutcome = FlowOutcome.Success;

        while (currentKey != null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!flow.Steps.TryGetValue(currentKey, out var stepDefinition))
            {
                throw new FlowExecutionException($"Step '{currentKey}' not found within flow '{flow.Name}'.");
            }

            FlowOutcome outcome;

            using var scope = context.Services.CreateScope();
            var step = (IFlowStep<TContext>)scope.ServiceProvider.GetRequiredService(stepDefinition.StepType);

            try
            {
                outcome = await step.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new FlowExecutionException(
                    $"Flow '{flow.Name}' step '{stepDefinition.StepType.FullName}' threw an exception.", ex);
            }

            lastOutcome = outcome;

            if (stepDefinition.Transitions.TryGetValue(outcome, out var nextStepKey))
            {
                currentKey = nextStepKey;
            }
            else
            {
                // No transition defined for this outcome - flow ends
                currentKey = null;
            }
        }

        return lastOutcome;
    }
}
