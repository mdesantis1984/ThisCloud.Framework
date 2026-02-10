using System;
using System.Collections.Generic;

namespace ThisCloud.Framework.Contracts.Exceptions;

/// <summary>
/// Base exception type for ThisCloud framework errors that carry an error code and an HTTP status.
/// </summary>
public abstract class ThisCloudException : Exception
{
    /// <summary>
    /// Application error code for the exception.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Corresponding HTTP status code.
    /// </summary>
    public int Status { get; }

    /// <summary>
    /// Optional validation errors dictionary (field -> messages).
    /// </summary>
    public IDictionary<string, string[]?>? ValidationErrors { get; }

    /// <summary>
    /// Creates a new instance of <see cref="ThisCloudException"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="code">The application error code.</param>
    /// <param name="status">The HTTP status code.</param>
    /// <param name="validationErrors">Optional validation errors.</param>
    protected ThisCloudException(string message, string code, int status, IDictionary<string, string[]?>? validationErrors = null)
        : base(message)
    {
        Code = code;
        Status = status;
        ValidationErrors = validationErrors;
    }
}

/// <summary>
/// Represents one or more validation failures.
/// </summary>
public class ValidationException : ThisCloudException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    /// <param name="message">The validation message.</param>
    /// <param name="errors">Optional validation errors dictionary.</param>
    public ValidationException(string message, IDictionary<string, string[]?>? errors = null)
        : base(message, "VALIDATION_ERROR", 400, errors)
    {
    }
}

/// <summary>
/// Indicates that a requested resource was not found.
/// </summary>
public class NotFoundException : ThisCloudException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    public NotFoundException(string message)
        : base(message, "NOT_FOUND", 404)
    {
    }
}

/// <summary>
/// Indicates a conflict, for example a concurrency or duplicate resource.
/// </summary>
public class ConflictException : ThisCloudException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictException"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    public ConflictException(string message)
        : base(message, "CONFLICT", 409)
    {
    }
}

/// <summary>
/// Indicates the operation is forbidden for the caller.
/// </summary>
public class ForbiddenException : ThisCloudException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForbiddenException"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    public ForbiddenException(string message)
        : base(message, "FORBIDDEN", 403)
    {
    }
}

/// <summary>
/// Helper factory for creating validation error dictionaries.
/// </summary>
public static class ValidationErrors
{
    /// <summary>
    /// Crea un diccionario de errores de validación a partir de una colección de tuplas (campo, mensajes).
    /// </summary>
    /// <param name="items">Array de tuplas donde cada elemento contiene el nombre del campo y sus mensajes asociados.</param>
    public static IDictionary<string, string[]?> From(params (string field, string[] messages)[] items)
    {
        var dict = new Dictionary<string, string[]?>();
        foreach (var (field, messages) in items)
        {
            dict[field] = messages;
        }

        return dict;
    }
}
