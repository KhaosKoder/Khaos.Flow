using Khaos.Flow.Abstractions;

namespace Khaos.Flow;

/// <summary>
/// Immutable description of a flow with steps and transitions.
/// </summary>
/// <typeparam name="TContext">The flow context type.</typeparam>
public sealed class FlowDefinition<TContext> : IFlowDefinition<TContext> 
    where TContext : IFlowContext
{
    private readonly IReadOnlyDictionary<string, FlowStepDefinition> _steps;

    /// <summary>
    /// Initializes a new flow definition.
    /// </summary>
    /// <param name="name">The flow name.</param>
    /// <param name="startStepKey">The key of the starting step.</param>
    /// <param name="steps">The step definitions.</param>
    internal FlowDefinition(
        string name,
        string startStepKey,
        IReadOnlyDictionary<string, FlowStepDefinition> steps)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        StartStepKey = startStepKey ?? throw new ArgumentNullException(nameof(startStepKey));
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string StartStepKey { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, IFlowStepDefinition> Steps => 
        _steps.ToDictionary(kvp => kvp.Key, kvp => (IFlowStepDefinition)kvp.Value);

    /// <summary>
    /// Gets the internal step definitions.
    /// </summary>
    internal IReadOnlyDictionary<string, FlowStepDefinition> InternalSteps => _steps;
}
