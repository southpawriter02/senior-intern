namespace AIntern.Desktop.Tests.TestHelpers;

/// <summary>
/// A synchronous dispatcher for unit testing that executes actions immediately.
/// </summary>
/// <remarks>
/// <para>
/// This implementation executes all actions synchronously on the calling thread,
/// making it suitable for unit tests where async UI thread behavior is not needed.
/// </para>
/// </remarks>
public sealed class TestDispatcher : IDispatcher
{
    /// <inheritdoc/>
    public Task InvokeAsync(Action action)
    {
        action();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<T> InvokeAsync<T>(Func<T> func)
    {
        return Task.FromResult(func());
    }

    /// <inheritdoc/>
    public Task InvokeAsync(Func<Task> action)
    {
        return action();
    }
}
