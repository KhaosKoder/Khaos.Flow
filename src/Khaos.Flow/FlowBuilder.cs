using Khaos.Flow.Abstractions;

namespace Khaos.Flow;

/// <summary>
/// Entry point for creating flows.
/// </summary>
public static class Flow
{
    /// <summary>
    /// Creates a new flow builder.
    /// </summary>
    /// <typeparam name="TContext">The flow context type.</typeparam>
    /// <param name="name">The flow name.</param>
    /// <returns>A new flow builder.</returns>
    public static FlowBuilder<TContext> Create<TContext>(string name) 
        where TContext : IFlowContext
    {
        return new FlowBuilder<TContext>(name);
    }
}

/// <summary>
/// Fluent builder for creating flow definitions.
/// </summary>
/// <typeparam name="TContext">The flow context type.</typeparam>
public sealed class FlowBuilder<TContext> : IFlowBuilder<TContext> 
    where TContext : IFlowContext
{
    private readonly string _name;
    private readonly Dictionary<Type, FlowStepBuilderState> _stepsByType = new();
    private readonly Dictionary<string, FlowStepBuilderState> _stepsByKey = new(StringComparer.Ordinal);
    private string? _startStepKey;
    private bool _built;

    internal FlowBuilder(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Flow name cannot be null or whitespace.", nameof(name));
        _name = name;
    }

    /// <inheritdoc />
    public IFlowStepBuilder<TContext> BeginWith<TStep>() where TStep : class, IFlowStep<TContext>
    {
        EnsureNotBuilt();
        
        if (_startStepKey != null)
            throw new InvalidOperationException($"Flow '{_name}' already has a starting step.");

        var step = GetOrCreateStep(typeof(TStep));
        _startStepKey = step.Key;
        return new FlowStepBuilder<TContext>(this, step);
    }

    /// <inheritdoc />
    public IFlowDefinition<TContext> Build()
    {
        EnsureNotBuilt();
        _built = true;

        if (_startStepKey is null)
            throw new InvalidOperationException($"Flow '{_name}' must define a starting step.");

        var steps = new Dictionary<string, FlowStepDefinition>(_stepsByKey.Count, StringComparer.Ordinal);
        foreach (var pair in _stepsByKey)
        {
            var transitions = pair.Value.Transitions
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Key);
            steps[pair.Key] = new FlowStepDefinition(pair.Value.StepType, transitions);
        }

        return new FlowDefinition<TContext>(_name, _startStepKey, steps);
    }

    internal FlowStepBuilderState GetOrCreateStep(Type stepType)
    {
        if (!_stepsByType.TryGetValue(stepType, out var step))
        {
            var key = stepType.FullName ?? stepType.Name;
            step = new FlowStepBuilderState(stepType, key);
            _stepsByType[stepType] = step;
            _stepsByKey[key] = step;
        }
        return step;
    }

    private void EnsureNotBuilt()
    {
        if (_built)
            throw new InvalidOperationException("Flow has already been built.");
    }
}

/// <summary>
/// Internal state for a step being built.
/// </summary>
internal sealed class FlowStepBuilderState
{
    public FlowStepBuilderState(Type stepType, string key)
    {
        StepType = stepType;
        Key = key;
    }

    public Type StepType { get; }
    public string Key { get; }
    public Dictionary<FlowOutcome, FlowStepBuilderState> Transitions { get; } = new();

    public void AddTransition(FlowOutcome outcome, FlowStepBuilderState target)
    {
        Transitions[outcome] = target;
    }
}

/// <summary>
/// Configures transitions for a flow step.
/// </summary>
/// <typeparam name="TContext">The flow context type.</typeparam>
public sealed class FlowStepBuilder<TContext> : IFlowStepBuilder<TContext> 
    where TContext : IFlowContext
{
    private readonly FlowBuilder<TContext> _flowBuilder;
    private readonly FlowStepBuilderState _currentStep;
    private FlowOutcome? _pendingOutcome;

    internal FlowStepBuilder(FlowBuilder<TContext> flowBuilder, FlowStepBuilderState currentStep)
    {
        _flowBuilder = flowBuilder;
        _currentStep = currentStep;
    }

    /// <inheritdoc />
    public IFlowStepBuilder<TContext> On(FlowOutcome outcome)
    {
        _pendingOutcome = outcome;
        return this;
    }

    /// <inheritdoc />
    public IFlowStepBuilder<TContext> Then<TStep>() where TStep : class, IFlowStep<TContext>
    {
        var nextStep = _flowBuilder.GetOrCreateStep(typeof(TStep));
        var outcome = _pendingOutcome ?? FlowOutcome.Success;
        _currentStep.AddTransition(outcome, nextStep);
        _pendingOutcome = null;
        return new FlowStepBuilder<TContext>(_flowBuilder, nextStep);
    }

    /// <inheritdoc />
    public IFlowStepBuilder<TContext> GoTo<TStep>() where TStep : class, IFlowStep<TContext>
    {
        var targetStep = _flowBuilder.GetOrCreateStep(typeof(TStep));
        var outcome = _pendingOutcome ?? FlowOutcome.Success;
        _currentStep.AddTransition(outcome, targetStep);
        _pendingOutcome = null;
        return this;
    }

    /// <inheritdoc />
    public IFlowBuilder<TContext> End()
    {
        return _flowBuilder;
    }

    /// <inheritdoc />
    public IFlowDefinition<TContext> Build()
    {
        return _flowBuilder.Build();
    }
}
