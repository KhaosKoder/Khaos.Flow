using Khaos.Flow.Abstractions;
using Khaos.Pipeline.Abstractions;

namespace Khaos.Flow.Adapters;

/// <summary>
/// Adapter that allows running a processing pipeline as a flow step.
/// This enables embedding data transformation pipelines within orchestration flows.
/// </summary>
/// <typeparam name="TContext">The flow context type.</typeparam>
/// <typeparam name="TIn">The pipeline input type.</typeparam>
/// <typeparam name="TOut">The pipeline output type.</typeparam>
public class PipelineFlowStep<TContext, TIn, TOut> : IFlowStep<TContext>
    where TContext : IFlowContext
{
    private readonly IProcessingPipeline<TIn, TOut> _pipeline;
    private readonly Func<TContext, IEnumerable<TIn>> _inputSelector;
    private readonly Func<IPipelineContext> _contextFactory;
    private readonly Func<IEnumerable<StepOutcome<TOut>>, FlowOutcome>? _outcomeMapper;
    private readonly Action<TContext, IEnumerable<StepOutcome<TOut>>>? _resultHandler;

    /// <summary>
    /// Initializes a new pipeline flow step adapter.
    /// </summary>
    /// <param name="pipeline">The pipeline to execute.</param>
    /// <param name="inputSelector">Function to extract inputs from the flow context.</param>
    /// <param name="contextFactory">Factory to create pipeline contexts.</param>
    /// <param name="outcomeMapper">Optional function to map pipeline results to flow outcome.</param>
    /// <param name="resultHandler">Optional handler for pipeline results.</param>
    public PipelineFlowStep(
        IProcessingPipeline<TIn, TOut> pipeline,
        Func<TContext, IEnumerable<TIn>> inputSelector,
        Func<IPipelineContext> contextFactory,
        Func<IEnumerable<StepOutcome<TOut>>, FlowOutcome>? outcomeMapper = null,
        Action<TContext, IEnumerable<StepOutcome<TOut>>>? resultHandler = null)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _inputSelector = inputSelector ?? throw new ArgumentNullException(nameof(inputSelector));
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _outcomeMapper = outcomeMapper;
        _resultHandler = resultHandler;
    }

    /// <inheritdoc />
    public async Task<FlowOutcome> ExecuteAsync(TContext context, CancellationToken cancellationToken)
    {
        var inputs = _inputSelector(context);
        var pipelineContext = _contextFactory();
        var results = new List<StepOutcome<TOut>>();

        foreach (var input in inputs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await _pipeline.ProcessAsync(input, pipelineContext, cancellationToken)
                .ConfigureAwait(false);
            results.Add(result);
        }

        _resultHandler?.Invoke(context, results);

        if (_outcomeMapper != null)
        {
            return _outcomeMapper(results);
        }

        // Default mapping: Success if all continue, Failure if any abort
        return results.All(r => r.IsContinue) ? FlowOutcome.Success : FlowOutcome.Failure;
    }
}

/// <summary>
/// Builder for creating <see cref="PipelineFlowStep{TContext, TIn, TOut}"/> instances.
/// </summary>
/// <typeparam name="TContext">The flow context type.</typeparam>
/// <typeparam name="TIn">The pipeline input type.</typeparam>
/// <typeparam name="TOut">The pipeline output type.</typeparam>
public sealed class PipelineFlowStepBuilder<TContext, TIn, TOut>
    where TContext : IFlowContext
{
    private readonly IProcessingPipeline<TIn, TOut> _pipeline;
    private Func<TContext, IEnumerable<TIn>>? _inputSelector;
    private Func<IPipelineContext>? _contextFactory;
    private Func<IEnumerable<StepOutcome<TOut>>, FlowOutcome>? _outcomeMapper;
    private Action<TContext, IEnumerable<StepOutcome<TOut>>>? _resultHandler;

    /// <summary>
    /// Initializes a new builder with the pipeline to adapt.
    /// </summary>
    /// <param name="pipeline">The pipeline to execute as a flow step.</param>
    public PipelineFlowStepBuilder(IProcessingPipeline<TIn, TOut> pipeline)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
    }

    /// <summary>
    /// Specifies how to extract inputs from the flow context.
    /// </summary>
    public PipelineFlowStepBuilder<TContext, TIn, TOut> WithInputs(Func<TContext, IEnumerable<TIn>> selector)
    {
        _inputSelector = selector ?? throw new ArgumentNullException(nameof(selector));
        return this;
    }

    /// <summary>
    /// Specifies the pipeline context factory.
    /// </summary>
    public PipelineFlowStepBuilder<TContext, TIn, TOut> WithContextFactory(Func<IPipelineContext> factory)
    {
        _contextFactory = factory ?? throw new ArgumentNullException(nameof(factory));
        return this;
    }

    /// <summary>
    /// Specifies how to map pipeline results to a flow outcome.
    /// </summary>
    public PipelineFlowStepBuilder<TContext, TIn, TOut> WithOutcomeMapper(
        Func<IEnumerable<StepOutcome<TOut>>, FlowOutcome> mapper)
    {
        _outcomeMapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        return this;
    }

    /// <summary>
    /// Specifies a handler for pipeline results.
    /// </summary>
    public PipelineFlowStepBuilder<TContext, TIn, TOut> WithResultHandler(
        Action<TContext, IEnumerable<StepOutcome<TOut>>> handler)
    {
        _resultHandler = handler ?? throw new ArgumentNullException(nameof(handler));
        return this;
    }

    /// <summary>
    /// Builds the pipeline flow step.
    /// </summary>
    public PipelineFlowStep<TContext, TIn, TOut> Build()
    {
        if (_inputSelector is null)
            throw new InvalidOperationException("Input selector must be specified.");

        var contextFactory = _contextFactory ?? (() => new DefaultPipelineContext());

        return new PipelineFlowStep<TContext, TIn, TOut>(
            _pipeline,
            _inputSelector,
            contextFactory,
            _outcomeMapper,
            _resultHandler);
    }
}
