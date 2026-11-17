using System.Runtime.CompilerServices;
using Serilog.Core;
using Serilog.Events;
using ThisCloud.Framework.Loggings.Abstractions;

namespace ThisCloud.Framework.Loggings.Serilog;

/// <summary>
/// Runtime state shared between the Serilog pipeline (built by <c>UseThisCloudFrameworkSerilog</c>) and
/// <see cref="SerilogLoggingControlService"/>: the per-namespace level overrides and the pipeline gate.
/// </summary>
/// <remarks>
/// Serilog can only add <c>MinimumLevel.Override</c> entries while the logger is being configured, so overrides
/// accepted at runtime are evaluated by <see cref="Filter"/> instead. To let overrides <em>lower</em> the level of a
/// namespace, the pipeline is gated by an internal switch that always sits at the lowest level that anything
/// (the global switch or an active override) allows; the filter then applies the exact per-event decision:
/// the most specific override whose name equals the event source context or is a namespace prefix of it, else the
/// global switch. Audit events (<see cref="SerilogAuditLogger"/> source) bypass the decision entirely.
/// The state is keyed by the global <see cref="LoggingLevelSwitch"/> so both sides find it without new public API.
/// </remarks>
internal sealed class RuntimeLogLevelOverrides
{
    internal const string AuditSourceContext = "ThisCloud.Framework.Loggings.Serilog.SerilogAuditLogger";

    private static readonly ConditionalWeakTable<LoggingLevelSwitch, RuntimeLogLevelOverrides> s_instances = new();

    private readonly LoggingLevelSwitch _global;
    private readonly object _sync = new();
    private OverrideEntry[] _entries = Array.Empty<OverrideEntry>();
    private bool _active = true;

    private RuntimeLogLevelOverrides(LoggingLevelSwitch globalLevelSwitch)
    {
        _global = globalLevelSwitch;
        Gate = new LoggingLevelSwitch(globalLevelSwitch.MinimumLevel);
        Filter = new LevelFilter(this);
        globalLevelSwitch.MinimumLevelChanged += (_, _) => RecomputeGate();
    }

    /// <summary>Gets the switch that gates the Serilog pipeline (lowest level anything allows).</summary>
    internal LoggingLevelSwitch Gate { get; }

    /// <summary>Gets the filter that applies the exact per-event level decision.</summary>
    internal ILogEventFilter Filter { get; }

    /// <summary>Gets the shared state for the given global level switch.</summary>
    internal static RuntimeLogLevelOverrides For(LoggingLevelSwitch globalLevelSwitch) =>
        s_instances.GetValue(globalLevelSwitch, static s => new RuntimeLogLevelOverrides(s));

    /// <summary>Replaces the overrides and whether they are active (they are ignored while logging is disabled).</summary>
    internal void Update(IReadOnlyDictionary<string, LogLevel>? overrides, bool active)
    {
        var entries = new List<OverrideEntry>();
        if (overrides != null)
        {
            foreach (var (name, level) in overrides)
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    entries.Add(new OverrideEntry(name, name + ".", ToSerilogLevel(level)));
                }
            }
        }

        // Most specific (longest) name first so the first match is the winner.
        var ordered = entries.OrderByDescending(e => e.Name.Length).ToArray();

        lock (_sync)
        {
            _entries = ordered;
            _active = active;
            RecomputeGate();
        }
    }

    internal static LogEventLevel ToSerilogLevel(LogLevel level) => level switch
    {
        LogLevel.Verbose => LogEventLevel.Verbose,
        LogLevel.Debug => LogEventLevel.Debug,
        LogLevel.Information => LogEventLevel.Information,
        LogLevel.Warning => LogEventLevel.Warning,
        LogLevel.Error => LogEventLevel.Error,
        LogLevel.Critical => LogEventLevel.Fatal,
        _ => LogEventLevel.Information
    };

    private void RecomputeGate()
    {
        lock (_sync)
        {
            var lowest = _global.MinimumLevel;
            if (_active)
            {
                foreach (var entry in _entries)
                {
                    if (entry.Level < lowest)
                    {
                        lowest = entry.Level;
                    }
                }
            }

            Gate.MinimumLevel = lowest;
        }
    }

    private bool IsEnabled(LogEvent logEvent)
    {
        string? source = null;
        if (logEvent.Properties.TryGetValue(Constants.SourceContextPropertyName, out var value)
            && value is ScalarValue { Value: string text })
        {
            source = text;
        }

        if (source == AuditSourceContext)
        {
            return true;
        }

        var level = _global.MinimumLevel;
        if (source != null)
        {
            OverrideEntry[] entries;
            bool active;
            lock (_sync)
            {
                entries = _entries;
                active = _active;
            }

            if (active)
            {
                foreach (var entry in entries)
                {
                    if (source.Equals(entry.Name, StringComparison.Ordinal)
                        || source.StartsWith(entry.Prefix, StringComparison.Ordinal))
                    {
                        level = entry.Level;
                        break;
                    }
                }
            }
        }

        return logEvent.Level >= level;
    }

    private readonly record struct OverrideEntry(string Name, string Prefix, LogEventLevel Level);

    private sealed class LevelFilter(RuntimeLogLevelOverrides owner) : ILogEventFilter
    {
        public bool IsEnabled(LogEvent logEvent) => owner.IsEnabled(logEvent);
    }
}
