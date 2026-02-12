using FluentAssertions;
using Xunit;

namespace ThisCloud.Framework.Loggings.Admin.Tests;

public sealed class LoggingsAdminPlaceholderTests
{
    [Fact]
    public void Message_ShouldReturnExpectedValue()
    {
        // Arrange
        var placeholder = new LoggingsAdminPlaceholder();

        // Act
        var message = placeholder.Message;

        // Assert
        message.Should().Be("ThisCloud.Framework.Loggings.Admin - Phase 0 complete");
    }
}
