namespace PromptFix.Api.Services;

public sealed class ModelConcurrencyGate : IModelConcurrencyGate
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<IDisposable?> TryEnterAsync(CancellationToken cancellationToken)
    {
        var entered = await _semaphore.WaitAsync(TimeSpan.Zero, cancellationToken);
        return entered ? new Releaser(_semaphore) : null;
    }

    private sealed class Releaser : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed;

        public Releaser(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _semaphore.Release();
            _disposed = true;
        }
    }
}
