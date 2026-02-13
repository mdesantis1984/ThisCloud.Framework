namespace ThisCloud.Framework.Loggings.Abstractions;

/// <summary>
/// Specifies the severity level of a log message.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Detailed trace information for debugging purposes.
    /// </summary>
    Verbose = 0,

    /// <summary>
    /// Debug information useful during development and troubleshooting.
    /// </summary>
    Debug = 1,

    /// <summary>
    /// Informational messages that track the normal flow of the application.
    /// </summary>
    Information = 2,

    /// <summary>
    /// Warning messages indicating potential issues that do not prevent operation.
    /// </summary>
    Warning = 3,

    /// <summary>
    /// Error messages indicating failures in the current operation.
    /// </summary>
    Error = 4,

    /// <summary>
    /// Critical failures that require immediate attention.
    /// </summary>
    Critical = 5
}
