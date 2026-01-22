using Khaos.Flow.Abstractions;

namespace Khaos.Flow;

/// <summary>
/// Immutable description of a step within a flow.
/// </summary>
public sealed class FlowStepDefinition : IFlowStepDefinition
{
    /// <summary>
    /// Initializes a new step definition.
    /// </summary>
    /// <param name="stepType">The type implementing the step.</param>
    /// <param name="transitions">Mapping from outcomes to next step keys.</param>
    public FlowStepDefinition(Type stepType, IReadOnlyDictionary<FlowOutcome, string> transitions)
    {
        StepType = stepType ?? throw new ArgumentNullException(nameof(stepType));
        Transitions = transitions ?? throw new ArgumentNullException(nameof(transitions));
    }

    /// <inheritdoc />
    public Type StepType { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<FlowOutcome, string> Transitions { get; }
}
