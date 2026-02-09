using FluentAssertions;
using ThisCloud.Framework.Contracts.Web;
using Xunit;

namespace ThisCloud.Framework.Contracts.Tests;

public class ThisCloudHeadersTests
{
    [Fact]
    public void Headers_constants_have_expected_values()
    {
        ThisCloudHeaders.CorrelationId.Should().Be("X-Correlation-Id");
        ThisCloudHeaders.RequestId.Should().Be("X-Request-Id");
    }
}
