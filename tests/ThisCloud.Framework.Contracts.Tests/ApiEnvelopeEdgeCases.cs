using System.Text.Json;
using FluentAssertions;
using ThisCloud.Framework.Contracts.Web;
using Xunit;

namespace ThisCloud.Framework.Contracts.Tests;

public class ApiEnvelopeEdgeCases
{
    [Fact]
    public void ApiEnvelope_with_null_data_and_errors_behaves()
    {
        var env = new ApiEnvelope<object?>
        {
            Meta = new Meta("svc", "v1", System.DateTime.UtcNow, System.Guid.NewGuid(), System.Guid.NewGuid(), null),
            Data = null,
            Errors = new System.Collections.Generic.List<ErrorItem>()
        };

        env.Data.Should().BeNull();
        env.Errors.Should().BeEmpty();

        var json = JsonSerializer.Serialize(env);
        var des = JsonSerializer.Deserialize<ApiEnvelope<object?>>(json);
        des.Should().NotBeNull();
        des!.Meta.Service.Should().Be("svc");
    }

    [Fact]
    public void ApiEnvelope_with_errors_serializes_correctly()
    {
        var err = new ErrorItem { Title = "t", Status = 400, Detail = "d" };
        var env = new ApiEnvelope<string>
        {
            Meta = new Meta("svc", "v1", System.DateTime.UtcNow, System.Guid.NewGuid(), System.Guid.NewGuid(), "tr"),
            Data = "ok",
            Errors = new System.Collections.Generic.List<ErrorItem> { err }
        };

        env.Errors.Count.Should().Be(1);
        var json = JsonSerializer.Serialize(env);
        json.Should().Contain("ok");
        var des = JsonSerializer.Deserialize<ApiEnvelope<string>>(json);
        des.Should().NotBeNull();
        des!.Errors.Should().HaveCount(1);
    }
}
