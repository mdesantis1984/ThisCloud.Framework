using FluentAssertions;
using Xunit;

namespace ThisCloud.Framework.Loggings.Abstractions.Tests;

public sealed class LoggingsAbstractionsPlaceholderTests
{
    [Fact]
    public void Message_ShouldReturnExpectedValue()
    {
        // Arrange
        var placeholder = new LoggingsAbstractionsPlaceholder();

        // Act
        var message = placeholder.Message;

        // Assert
        message.Should().Be("ThisCloud.Framework.Loggings.Abstractions - Phase 0 complete");
    }
}
