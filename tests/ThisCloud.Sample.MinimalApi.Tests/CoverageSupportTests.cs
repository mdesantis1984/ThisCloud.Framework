using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using ThisCloud.Framework.Contracts.Web;
using ThisCloud.Framework.Web.Results;
using Xunit;

namespace ThisCloud.Sample.MinimalApi.Tests;

/// <summary>
/// Coverage support tests for Contracts and Web components NOT exercised by smoke tests.
/// These tests ensure >=90% line coverage when Sample.MinimalApi.Tests measures all referenced assemblies (Contracts, Web, MinimalApi).
/// </summary>
public class CoverageSupportTests
{
    /// <summary>
    /// Meta parameterless constructor should initialize with default values.
    /// Coverage gap: Meta.cs line 10 (.ctor())
    /// </summary>
    [Fact]
    public void Meta_ParameterlessConstructor_InitializesWithDefaults()
    {
        // Act
        var meta = new Meta();

        // Assert
        meta.Should().NotBeNull();
        meta.Service.Should().Be(string.Empty); // Initialized to string.Empty, not null
        meta.Version.Should().Be(string.Empty); // Initialized to string.Empty, not null
        meta.TimestampUtc.Should().Be(default(DateTimeOffset));
        meta.CorrelationId.Should().Be(default(Guid));
        meta.RequestId.Should().Be(default(Guid));
        meta.TraceId.Should().BeNull();
    }

    /// <summary>
    /// ProblemDetailsDto parameterless constructor should initialize with defaults.
    /// Coverage gap: ProblemDetailsDto.cs line 15 (.ctor())
    /// </summary>
    [Fact]
    public void ProblemDetailsDto_ParameterlessConstructor_InitializesWithDefaults()
    {
        // Act
        var dto = new ProblemDetailsDto();

        // Assert
        dto.Should().NotBeNull();
        dto.Type.Should().BeNull();
        dto.Title.Should().BeNull();
        dto.Status.Should().Be(0);
        dto.Detail.Should().BeNull();
        dto.Instance.Should().BeNull();
        dto.Extensions.Should().NotBeNull().And.BeEmpty();
    }

    /// <summary>
    /// ThisCloudResults.SeeOther should return 303 status code.
    /// Coverage gap: ThisCloudResults.cs line 49-53 (SeeOther method)
    /// </summary>
    [Fact]
    public void ThisCloudResults_SeeOther_Returns303StatusCode()
    {
        // Arrange
        var location = "/api/users/123";

        // Act
        var result = ThisCloudResults.SeeOther(location);

        // Assert
        result.Should().NotBeNull();
        var statusCodeResult = result.Should().BeOfType<StatusCodeHttpResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(303);
    }

    /// <summary>
    /// ThisCloudResults.BadRequest should return 400 with validation errors in envelope.
    /// Coverage gap: ThisCloudResults.cs line 65-75 (BadRequest method)
    /// </summary>
    [Fact]
    public void ThisCloudResults_BadRequest_Returns400WithValidationErrors()
    {
        // Arrange
        var validationErrors = new Dictionary<string, string[]?>
        {
            ["email"] = new[] { "Invalid format" },
            ["age"] = new[] { "Must be positive" }
        };

        // Act
        var result = ThisCloudResults.BadRequest(
            "VALIDATION_FAILED",
            "Validation Error",
            "Request validation failed",
            validationErrors,
            "test-svc",
            "v1");

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Should().BeOfType<BadRequest<ApiEnvelope<object?>>>().Subject;
        badRequestResult.Value.Should().NotBeNull();
        badRequestResult.Value!.Errors.Should().HaveCount(1);
        badRequestResult.Value.Errors[0].Status.Should().Be(400);
        badRequestResult.Value.Errors[0].Title.Should().Be("Validation Error");
        badRequestResult.Value.Errors[0].Extensions.Should().ContainKey("errors");
    }

    /// <summary>
    /// ThisCloudResults.Unauthorized should return 401 status code.
    /// Coverage gap: ThisCloudResults.cs line 84-88 (Unauthorized method)
    /// </summary>
    [Fact]
    public void ThisCloudResults_Unauthorized_Returns401StatusCode()
    {
        // Arrange
        var detail = "Bearer token expired";

        // Act
        var result = ThisCloudResults.Unauthorized(detail, "auth-svc", "v1");

        // Assert
        result.Should().NotBeNull();
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedHttpResult>().Subject;
        // UnauthorizedHttpResult doesn't expose value, but status code is 401 by type
    }

    /// <summary>
    /// ThisCloudResults.Forbidden should return 403 with error envelope.
    /// Coverage gap: ThisCloudResults.cs line 97-102 (Forbidden method)
    /// </summary>
    [Fact]
    public void ThisCloudResults_Forbidden_Returns403WithErrorEnvelope()
    {
        // Arrange
        var detail = "Insufficient permissions";

        // Act
        var result = ThisCloudResults.Forbidden(detail, "authz-svc", "v1");

        // Assert
        result.Should().NotBeNull();
        var jsonResult = result.Should().BeOfType<JsonHttpResult<ApiEnvelope<object?>>>().Subject;
        jsonResult.StatusCode.Should().Be(403);
        jsonResult.Value.Should().NotBeNull();
        jsonResult.Value!.Errors.Should().HaveCount(1);
        jsonResult.Value.Errors[0].Status.Should().Be(403);
        jsonResult.Value.Errors[0].Title.Should().Be("Forbidden");
    }

    /// <summary>
    /// ThisCloudResults.UpstreamFailure should return 502 with error envelope.
    /// Coverage gap: ThisCloudResults.cs line 138-143 (UpstreamFailure method)
    /// </summary>
    [Fact]
    public void ThisCloudResults_UpstreamFailure_Returns502WithErrorEnvelope()
    {
        // Arrange
        var detail = "Payment gateway connection failed";

        // Act
        var result = ThisCloudResults.UpstreamFailure(detail, "payment-svc", "v1");

        // Assert
        result.Should().NotBeNull();
        var jsonResult = result.Should().BeOfType<JsonHttpResult<ApiEnvelope<object?>>>().Subject;
        jsonResult.StatusCode.Should().Be(502);
        jsonResult.Value.Should().NotBeNull();
        jsonResult.Value!.Errors.Should().HaveCount(1);
        jsonResult.Value.Errors[0].Status.Should().Be(502);
        jsonResult.Value.Errors[0].Title.Should().Be("Bad Gateway");
    }

    /// <summary>
    /// ThisCloudResults.Unhandled should return 500 with error envelope.
    /// Coverage gap: ThisCloudResults.cs line 152-157 (Unhandled method)
    /// </summary>
    [Fact]
    public void ThisCloudResults_Unhandled_Returns500WithErrorEnvelope()
    {
        // Arrange
        var detail = "Unexpected database error";

        // Act
        var result = ThisCloudResults.Unhandled(detail, "data-svc", "v1");

        // Assert
        result.Should().NotBeNull();
        var jsonResult = result.Should().BeOfType<JsonHttpResult<ApiEnvelope<object?>>>().Subject;
        jsonResult.StatusCode.Should().Be(500);
        jsonResult.Value.Should().NotBeNull();
        jsonResult.Value!.Errors.Should().HaveCount(1);
        jsonResult.Value.Errors[0].Status.Should().Be(500);
        jsonResult.Value.Errors[0].Title.Should().Be("Internal Server Error");
    }

    /// <summary>
    /// ThisCloudResults.UpstreamTimeout should return 504 with error envelope.
    /// Coverage gap: ThisCloudResults.cs line 166-171 (UpstreamTimeout method)
    /// </summary>
    [Fact]
    public void ThisCloudResults_UpstreamTimeout_Returns504WithErrorEnvelope()
    {
        // Arrange
        var detail = "External API timeout after 30s";

        // Act
        var result = ThisCloudResults.UpstreamTimeout(detail, "integration-svc", "v1");

        // Assert
        result.Should().NotBeNull();
        var jsonResult = result.Should().BeOfType<JsonHttpResult<ApiEnvelope<object?>>>().Subject;
        jsonResult.StatusCode.Should().Be(504);
        jsonResult.Value.Should().NotBeNull();
        jsonResult.Value!.Errors.Should().HaveCount(1);
        jsonResult.Value.Errors[0].Status.Should().Be(504);
        jsonResult.Value.Errors[0].Title.Should().Be("Gateway Timeout");
    }
}
