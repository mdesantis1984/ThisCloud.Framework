namespace ThisCloud.Framework.Loggings.Abstractions;

/// <summary>
/// Root configuration settings for ThisCloud.Framework.Loggings.
/// </summary>
public sealed class LogSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether logging is enabled.
    /// </summary>
    /// <remarks>
    /// Default: <c>true</c>.
    /// </remarks>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum log level.
    /// </summary>
    /// <remarks>
    /// Default: <see cref="LogLevel.Information"/>.
    /// </remarks>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Gets or sets log level overrides by namespace or source context.
    /// </summary>
    /// <remarks>
    /// Optional. Key: namespace/source context; Value: log level.
    /// Example: { "MyApp.Database": LogLevel.Debug }
    /// </remarks>
    public IReadOnlyDictionary<string, LogLevel>? Overrides { get; set; }

    /// <summary>
    /// Gets or sets the console sink configuration.
    /// </summary>
    public ConsoleSinkSettings Console { get; set; } = new();

    /// <summary>
    /// Gets or sets the file sink configuration.
    /// </summary>
    public FileSinkSettings File { get; set; } = new();

    /// <summary>
    /// Gets or sets the retention policy configuration.
    /// </summary>
    public RetentionSettings Retention { get; set; } = new();

    /// <summary>
    /// Gets or sets the redaction configuration.
    /// </summary>
    public RedactionSettings Redaction { get; set; } = new();

    /// <summary>
    /// Gets or sets the correlation tracking configuration.
    /// </summary>
    public CorrelationSettings Correlation { get; set; } = new();
}
