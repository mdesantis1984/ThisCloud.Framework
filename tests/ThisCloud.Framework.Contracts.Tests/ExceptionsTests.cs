using FluentAssertions;
using ThisCloud.Framework.Contracts.Exceptions;
using Xunit;

namespace ThisCloud.Framework.Contracts.Tests;

public class ExceptionsTests
{
    [Fact]
    public void Exceptions_have_code_and_status()
    {
        var ex = new NotFoundException("x");
        ex.Code.Should().Be("NOT_FOUND");
        ex.Status.Should().Be(404);

        var v = new ValidationException("m", new System.Collections.Generic.Dictionary<string, string[]?>{{"f", new[]{"e"}}});
        v.Code.Should().Be("VALIDATION_ERROR");
        v.Status.Should().Be(400);
        v.ValidationErrors.Should().ContainKey("f");
    }

    /// <summary>
    /// ConflictException should have correct code and status.
    /// Coverage gap: Exceptions.cs line 76-86 (ConflictException)
    /// </summary>
    [Fact]
    public void ConflictException_HasCorrectCodeAndStatus()
    {
        // Act
        var ex = new ConflictException("Resource already exists");

        // Assert
        ex.Code.Should().Be("CONFLICT");
        ex.Status.Should().Be(409);
        ex.Message.Should().Be("Resource already exists");
        ex.ValidationErrors.Should().BeNull();
    }

    /// <summary>
    /// ForbiddenException should have correct code and status.
    /// Coverage gap: Exceptions.cs line 91-101 (ForbiddenException)
    /// </summary>
    [Fact]
    public void ForbiddenException_HasCorrectCodeAndStatus()
    {
        // Act
        var ex = new ForbiddenException("Access denied to resource");

        // Assert
        ex.Code.Should().Be("FORBIDDEN");
        ex.Status.Should().Be(403);
        ex.Message.Should().Be("Access denied to resource");
        ex.ValidationErrors.Should().BeNull();
    }

    /// <summary>
    /// ValidationErrors.From() should create dictionary from tuples.
    /// Coverage gap: Exceptions.cs line 112-120 (ValidationErrors.From)
    /// </summary>
    [Fact]
    public void ValidationErrors_From_CreatesDictionaryFromTuples()
    {
        // Act
        var errors = ValidationErrors.From(
            ("email", new[] { "Invalid format", "Already exists" }),
            ("password", new[] { "Too short" }),
            ("age", new[] { "Must be positive" })
        );

        // Assert
        errors.Should().HaveCount(3);
        errors["email"].Should().BeEquivalentTo(new[] { "Invalid format", "Already exists" });
        errors["password"].Should().BeEquivalentTo(new[] { "Too short" });
        errors["age"].Should().BeEquivalentTo(new[] { "Must be positive" });
    }
}
