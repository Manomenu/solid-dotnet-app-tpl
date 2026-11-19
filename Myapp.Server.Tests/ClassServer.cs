namespace Myapp.Server.Tests;

public sealed class ClassServer : IDisposable
{
    internal TestApp App { get; } = new();

    public void Dispose() => App.Dispose();
}
