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

        var v = new ValidationException("m", new System.Collections.Generic.Dictionary<string, string[]>{{"f", new[]{"e"}}});
        v.Code.Should().Be("VALIDATION_ERROR");
        v.Status.Should().Be(400);
        v.ValidationErrors.Should().ContainKey("f");
    }
}
