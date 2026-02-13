using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using ThisCloud.Framework.Loggings.Abstractions;
using Xunit;

namespace ThisCloud.Framework.Loggings.Serilog.Tests;

/// <summary>
/// Tests for <see cref="DefaultCorrelationContext"/> (internal class in ServiceCollectionExtensions.cs).
/// Tests via DI to respect internal visibility.
/// </summary>
public sealed class DefaultCorrelationContextTests
{
    private static ICorrelationContext CreateDefaultCorrelationContext()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddThisCloudFrameworkLoggings(configuration, "test-service");
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ICorrelationContext>();
    }

    [Fact]
    public void CorrelationId_ReturnsNull()
    {
        // Arrange
        var context = CreateDefaultCorrelationContext();

        // Act
        var result = context.CorrelationId;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void RequestId_ReturnsNull()
    {
        // Arrange
        var context = CreateDefaultCorrelationContext();

        // Act
        var result = context.RequestId;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TraceId_WithNoActivity_ReturnsNull()
    {
        // Arrange
        Activity.Current = null; // Ensure no ambient activity
        var context = CreateDefaultCorrelationContext();

        // Act
        var result = context.TraceId;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TraceId_WithActivity_ReturnsActivityTraceId()
    {
        // Arrange
        using var activity = new Activity("test-operation").Start();
        var context = CreateDefaultCorrelationContext();

        // Act
        var result = context.TraceId;

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Be(activity.TraceId.ToString());
    }

    [Fact]
    public void UserId_ReturnsNull()
    {
        // Arrange
        var context = CreateDefaultCorrelationContext();

        // Act
        var result = context.UserId;

        // Assert
        result.Should().BeNull();
    }
}
