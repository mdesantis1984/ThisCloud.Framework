namespace ThisCloud.Framework.Loggings.Abstractions;

/// <summary>
/// Configuration settings for the file sink.
/// </summary>
public sealed class FileSinkSettings
{
    private int _rollingFileSizeMb = 10;
    private int _retainedFileCountLimit = 30;

    /// <summary>
    /// Gets or sets a value indicating whether the file sink is enabled.
    /// </summary>
    /// <remarks>
    /// Default: <c>true</c>.
    /// </remarks>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the log file path pattern.
    /// </summary>
    /// <remarks>
    /// Default: <c>"logs/log-.ndjson"</c>.
    /// The pattern can include date/time placeholders for rolling files.
    /// </remarks>
    public string Path { get; set; } = "logs/log-.ndjson";

    /// <summary>
    /// Gets or sets the maximum file size in megabytes before rolling to a new file.
    /// </summary>
    /// <remarks>
    /// Default: <c>10</c> MB.
    /// Valid range: 1 to 100 MB.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value is less than 1 or greater than 100.
    /// </exception>
    public int RollingFileSizeMb
    {
        get => _rollingFileSizeMb;
        set
        {
            if (value < 1 || value > 100)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(RollingFileSizeMb),
                    value,
                    "RollingFileSizeMb must be between 1 and 100.");
            }
            _rollingFileSizeMb = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum number of log files to retain.
    /// </summary>
    /// <remarks>
    /// Default: <c>30</c>.
    /// Valid range: 1 to 365.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value is less than 1 or greater than 365.
    /// </exception>
    public int RetainedFileCountLimit
    {
        get => _retainedFileCountLimit;
        set
        {
            if (value < 1 || value > 365)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(RetainedFileCountLimit),
                    value,
                    "RetainedFileCountLimit must be between 1 and 365.");
            }
            _retainedFileCountLimit = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to use compact JSON (NDJSON) format.
    /// </summary>
    /// <remarks>
    /// Default: <c>true</c>.
    /// When enabled, logs are written in newline-delimited JSON format for efficient parsing.
    /// </remarks>
    public bool UseCompactJson { get; set; } = true;
}
