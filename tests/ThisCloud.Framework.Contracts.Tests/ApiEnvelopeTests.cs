using System.Text.Json;
using FluentAssertions;
using ThisCloud.Framework.Contracts.Web;
using Xunit;

namespace ThisCloud.Framework.Contracts.Tests;

public class ApiEnvelopeTests
{
    [Fact]
    public void ApiEnvelope_serializes_and_deserializes()
    {
        var env = new ApiEnvelope<string>
        {
            Meta = new Meta("svc", "v1", System.DateTime.UtcNow, System.Guid.NewGuid(), System.Guid.NewGuid(), "t"),
            Data = "hello",
            Errors = new System.Collections.Generic.List<ErrorItem>()
        };

        var json = JsonSerializer.Serialize(env);
        var des = JsonSerializer.Deserialize<ApiEnvelope<string>>(json);
        des.Should().NotBeNull();
        des!.Data.Should().Be("hello");
    }
}
