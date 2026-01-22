using System.Collections.Concurrent;
using Khaos.Pipeline.Abstractions;

namespace Khaos.Flow.Adapters;

/// <summary>
/// Default implementation of <see cref="IPipelineContext"/>.
/// </summary>
internal sealed class DefaultPipelineContext : IPipelineContext
{
    private readonly ConcurrentDictionary<string, object?> _items = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Items => _items;

    /// <inheritdoc />
    public T Get<T>(string key)
    {
        if (key is null)
            throw new ArgumentNullException(nameof(key));

        if (!_items.TryGetValue(key, out var value))
            throw new KeyNotFoundException($"PipelineContext value '{key}' was not found.");

        return (T)value!;
    }

    /// <inheritdoc />
    public bool TryGet<T>(string key, out T? value)
    {
        if (key is null)
            throw new ArgumentNullException(nameof(key));

        if (_items.TryGetValue(key, out var raw) && raw is T typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }

    /// <inheritdoc />
    public void Set(string key, object? value)
    {
        if (key is null)
            throw new ArgumentNullException(nameof(key));

        _items[key] = value;
    }
}
