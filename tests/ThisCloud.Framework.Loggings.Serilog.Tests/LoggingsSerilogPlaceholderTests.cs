using FluentAssertions;
using Xunit;

namespace ThisCloud.Framework.Loggings.Serilog.Tests;

public sealed class LoggingsSerilogPlaceholderTests
{
    [Fact]
    public void Message_ShouldReturnExpectedValue()
    {
        // Arrange
        var placeholder = new LoggingsSerilogPlaceholder();

        // Act
        var message = placeholder.Message;

        // Assert
        message.Should().Be("ThisCloud.Framework.Loggings.Serilog - Phase 0 complete");
    }
}
