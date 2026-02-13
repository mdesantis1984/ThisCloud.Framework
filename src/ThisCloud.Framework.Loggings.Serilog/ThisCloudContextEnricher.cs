using Serilog.Core;
using Serilog.Events;
using System.Diagnostics;
using ThisCloud.Framework.Loggings.Abstractions;

namespace ThisCloud.Framework.Loggings.Serilog;

/// <summary>
/// Serilog enricher that adds ThisCloud.Framework correlation and context properties to log events.
/// </summary>
/// <remarks>
/// Properties are emitted only when their values are non-null/non-empty.
/// TraceId is sourced from <see cref="Activity.Current"/> when available.
/// Other correlation values come from <see cref="ICorrelationContext"/>.
/// </remarks>
public sealed class ThisCloudContextEnricher : ILogEventEnricher
{
    private readonly ICorrelationContext _correlationContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThisCloudContextEnricher"/> class.
    /// </summary>
    /// <param name="correlationContext">Correlation context provider.</param>
    public ThisCloudContextEnricher(ICorrelationContext correlationContext)
    {
        _correlationContext = correlationContext ?? throw new ArgumentNullException(nameof(correlationContext));
    }

    /// <summary>
    /// Enriches the log event with correlation and context properties.
    /// </summary>
    /// <param name="logEvent">The log event to enrich.</param>
    /// <param name="propertyFactory">Factory for creating log event properties.</param>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(propertyFactory);

        // CorrelationId (GUID)
        if (_correlationContext.CorrelationId.HasValue)
        {
            var property = propertyFactory.CreateProperty(
                ThisCloudLogKeys.CorrelationId,
                _correlationContext.CorrelationId.Value);
            logEvent.AddPropertyIfAbsent(property);
        }

        // RequestId (GUID)
        if (_correlationContext.RequestId.HasValue)
        {
            var property = propertyFactory.CreateProperty(
                ThisCloudLogKeys.RequestId,
                _correlationContext.RequestId.Value);
            logEvent.AddPropertyIfAbsent(property);
        }

        // TraceId (W3C from Activity)
        var traceId = Activity.Current?.TraceId.ToString();
        if (!string.IsNullOrWhiteSpace(traceId))
        {
            var property = propertyFactory.CreateProperty(ThisCloudLogKeys.TraceId, traceId);
            logEvent.AddPropertyIfAbsent(property);
        }

        // UserId (string)
        if (!string.IsNullOrWhiteSpace(_correlationContext.UserId))
        {
            var property = propertyFactory.CreateProperty(
                ThisCloudLogKeys.UserId,
                _correlationContext.UserId);
            logEvent.AddPropertyIfAbsent(property);
        }
    }
}
