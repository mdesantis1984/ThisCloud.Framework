using FluentAssertions;
using System.Text.Json;
using ThisCloud.Framework.Contracts.Web;
using Xunit;

namespace ThisCloud.Framework.Web.Tests;

public class ContractsCoverageFromWebTests
{
    [Fact]
    public void UseContracts_Types_From_WebTests()
    {
        var meta = new Meta("websvc", "1.0", System.DateTime.UtcNow, System.Guid.NewGuid(), System.Guid.NewGuid(), null);
        var ei = new ErrorItem { Title = "Err", Detail = "d", Status = 500 };
        var env = new ApiEnvelope<ErrorItem> { Meta = meta, Data = ei };

        env.Data.Should().NotBeNull();
        env.Meta.Service.Should().Be("websvc");

        var json = JsonSerializer.Serialize(env);
        var round = JsonSerializer.Deserialize<ApiEnvelope<ErrorItem>>(json);
        round.Should().NotBeNull();
        round!.Data.Should().NotBeNull();
    }
}
