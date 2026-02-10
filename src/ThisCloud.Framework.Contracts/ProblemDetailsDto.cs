using System.Collections.Generic;

namespace ThisCloud.Framework.Contracts.Web;

/// <summary>
/// Represents a single error item compatible with ProblemDetails semantics.
/// </summary>
public class ErrorItem
{
    /// <summary>
    /// A URI reference that identifies the problem type.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Short, human-readable summary of the problem type.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The HTTP status code applicable to this problem.
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Detailed human-readable explanation specific to this occurrence of the problem.
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// A URI reference that identifies the specific occurrence of the problem.
    /// </summary>
    public string? Instance { get; set; }

    /// <summary>
    /// Extension members for additional problem details.
    /// </summary>
    public Dictionary<string, object?> Extensions { get; set; } = new();

    /// <summary>
    /// Helper to add an extension value.
    /// </summary>
    public void AddExtension(string key, object? value) => Extensions[key] = value;
}

/// <summary>
/// DTO matching ProblemDetails shape extended for this framework.
/// </summary>
public class ProblemDetailsDto : ErrorItem
{
    /// <summary>
    /// Creates a new ProblemDetailsDto with a default extensions dictionary.
    /// </summary>
    public ProblemDetailsDto()
    {
        Extensions = Extensions ?? new Dictionary<string, object?>();
    }
}
