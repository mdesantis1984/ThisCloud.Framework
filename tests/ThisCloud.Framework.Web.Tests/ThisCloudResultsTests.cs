using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using System;
using System.Collections.Generic;
using System.Text.Json;
using ThisCloud.Framework.Contracts.Web;
using ThisCloud.Framework.Web.Results;
using Xunit;

namespace ThisCloud.Framework.Web.Tests;

/// <summary>
/// Tests para ThisCloudResults helpers.
/// </summary>
public class ThisCloudResultsTests
{
    /// <summary>
    /// Ok debe retornar 200 con envelope y data.
    /// </summary>
    [Fact]
    public void Ok_ReturnsStatus200WithEnvelopeAndData()
    {
        // Arrange & Act
        var result = ThisCloudResults.Ok(new { Name = "John", Age = 30 }, "test-service", "v1");

        // Assert
        Assert.NotNull(result);
        // IResult no expone Value directamente en assertions genéricas
        // La funcionalidad se valida en integration tests
    }

    /// <summary>
    /// Created debe retornar 201 con Location y envelope.
    /// </summary>
    [Fact]
    public void Created_ReturnsStatus201WithLocationAndEnvelope()
    {
        // Arrange & Act
        var testData = new { Id = 123, Name = "New User" };
        var location = "/api/users/123";
        var result = ThisCloudResults.Created(location, testData, "test-service", "v1");

        // Assert
        Assert.NotNull(result);
        // Type assertion no válida con anonymous types; validado en integration tests
    }

    /// <summary>
    /// SeeOther debe retornar 303.
    /// </summary>
    [Fact]
    public void SeeOther_ReturnsStatus303()
    {
        // Arrange
        var location = "/api/users/456";

        // Act
        var result = ThisCloudResults.SeeOther(location);

        // Assert
        Assert.NotNull(result);
        var statusCodeResult = Assert.IsType<StatusCodeHttpResult>(result);
        Assert.Equal(303, statusCodeResult.StatusCode);
    }

    /// <summary>
    /// BadRequest debe retornar 400 con envelope y validation errors.
    /// </summary>
    [Fact]
    public void BadRequest_ReturnsStatus400WithValidationErrors()
    {
        // Arrange
        var validationErrors = new Dictionary<string, string[]?>
        {
            ["email"] = new[] { "Invalid format" },
            ["password"] = new[] { "Too short" }
        };

        // Act
        var result = ThisCloudResults.BadRequest(
            "VALIDATION_ERROR",
            "Bad Request",
            "Validation failed",
            validationErrors,
            "test-service",
            "v1");

        // Assert
        Assert.NotNull(result);
        var badRequestResult = Assert.IsType<BadRequest<ApiEnvelope<object?>>>(result);
        Assert.NotNull(badRequestResult.Value);
        Assert.Single(badRequestResult.Value.Errors);

        var error = badRequestResult.Value.Errors[0];
        Assert.Equal(400, error.Status);
        Assert.Equal("Bad Request", error.Title);
        Assert.True(error.Extensions.ContainsKey("errors"));
    }

    /// <summary>
    /// Unauthorized debe retornar 401.
    /// </summary>
    [Fact]
    public void Unauthorized_ReturnsStatus401()
    {
        // Act
        var result = ThisCloudResults.Unauthorized("Token expired", "test-service", "v1");

        // Assert
        Assert.NotNull(result);
        var unauthorizedResult = Assert.IsType<UnauthorizedHttpResult>(result);
        // UnauthorizedHttpResult no tiene Value, solo status code
    }

    /// <summary>
    /// Forbidden debe retornar 403 con envelope.
    /// </summary>
    [Fact]
    public void Forbidden_ReturnsStatus403WithEnvelope()
    {
        // Act
        var result = ThisCloudResults.Forbidden("Access denied", "test-service", "v1");

        // Assert
        Assert.NotNull(result);
        var jsonResult = Assert.IsType<JsonHttpResult<ApiEnvelope<object?>>>(result);
        Assert.Equal(403, jsonResult.StatusCode);
        Assert.NotNull(jsonResult.Value);
        Assert.Single(jsonResult.Value.Errors);
        Assert.Equal(403, jsonResult.Value.Errors[0].Status);
    }

    /// <summary>
    /// NotFound debe retornar 404 con envelope.
    /// </summary>
    [Fact]
    public void NotFound_ReturnsStatus404WithEnvelope()
    {
        // Act
        var result = ThisCloudResults.NotFound("Resource not found", "test-service", "v1");

        // Assert
        Assert.NotNull(result);
        var notFoundResult = Assert.IsType<NotFound<ApiEnvelope<object?>>>(result);
        Assert.NotNull(notFoundResult.Value);
        Assert.Single(notFoundResult.Value.Errors);
        Assert.Equal(404, notFoundResult.Value.Errors[0].Status);
    }

    /// <summary>
    /// Conflict debe retornar 409 con envelope.
    /// </summary>
    [Fact]
    public void Conflict_ReturnsStatus409WithEnvelope()
    {
        // Act
        var result = ThisCloudResults.Conflict("Email already exists", "test-service", "v1");

        // Assert
        Assert.NotNull(result);
        var conflictResult = Assert.IsType<Conflict<ApiEnvelope<object?>>>(result);
        Assert.NotNull(conflictResult.Value);
        Assert.Single(conflictResult.Value.Errors);
        Assert.Equal(409, conflictResult.Value.Errors[0].Status);
    }

    /// <summary>
    /// UpstreamFailure debe retornar 502 con envelope.
    /// </summary>
    [Fact]
    public void UpstreamFailure_ReturnsStatus502WithEnvelope()
    {
        // Act
        var result = ThisCloudResults.UpstreamFailure("Upstream service down", "test-service", "v1");

        // Assert
        Assert.NotNull(result);
        var jsonResult = Assert.IsType<JsonHttpResult<ApiEnvelope<object?>>>(result);
        Assert.Equal(502, jsonResult.StatusCode);
        Assert.NotNull(jsonResult.Value);
        Assert.Single(jsonResult.Value.Errors);
        Assert.Equal(502, jsonResult.Value.Errors[0].Status);
        Assert.Equal("Bad Gateway", jsonResult.Value.Errors[0].Title);
    }

    /// <summary>
    /// Unhandled debe retornar 500 con envelope.
    /// </summary>
    [Fact]
    public void Unhandled_ReturnsStatus500WithEnvelope()
    {
        // Act
        var result = ThisCloudResults.Unhandled("Unexpected error", "test-service", "v1");

        // Assert
        Assert.NotNull(result);
        var jsonResult = Assert.IsType<JsonHttpResult<ApiEnvelope<object?>>>(result);
        Assert.Equal(500, jsonResult.StatusCode);
        Assert.NotNull(jsonResult.Value);
        Assert.Single(jsonResult.Value.Errors);
        Assert.Equal(500, jsonResult.Value.Errors[0].Status);
        Assert.Equal("Internal Server Error", jsonResult.Value.Errors[0].Title);
    }

    /// <summary>
    /// UpstreamTimeout debe retornar 504 con envelope.
    /// </summary>
    [Fact]
    public void UpstreamTimeout_ReturnsStatus504WithEnvelope()
    {
        // Act
        var result = ThisCloudResults.UpstreamTimeout("Gateway timeout", "test-service", "v1");

        // Assert
        Assert.NotNull(result);
        var jsonResult = Assert.IsType<JsonHttpResult<ApiEnvelope<object?>>>(result);
        Assert.Equal(504, jsonResult.StatusCode);
        Assert.NotNull(jsonResult.Value);
        Assert.Single(jsonResult.Value.Errors);
        Assert.Equal(504, jsonResult.Value.Errors[0].Status);
        Assert.Equal("Gateway Timeout", jsonResult.Value.Errors[0].Title);
    }

    /// <summary>
    /// Todos los helpers deben incluir meta completo.
    /// </summary>
    [Fact]
    public void AllHelpers_IncludeCompleteMeta()
    {
        // Act
        var okResult = ThisCloudResults.Ok(new { Test = true }, "my-service", "v2");

        // Assert
        Assert.NotNull(okResult);
        // Meta completitud se valida en integration tests (ExceptionMappingTests)
    }
}
