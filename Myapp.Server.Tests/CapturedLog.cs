using Microsoft.Extensions.Logging;

namespace Myapp.Server.Tests;

internal sealed record CapturedLog(string Category, LogLevel Level, string Message, string? TraceId);
