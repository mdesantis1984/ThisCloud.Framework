using System.Text.Json;
using FluentAssertions;
using ThisCloud.Framework.Contracts.Web;
using Xunit;

namespace ThisCloud.Framework.Contracts.Tests;

public class ProblemDetailsTests
{
    [Fact]
    public void ProblemDetails_serializes_and_contains_extensions()
    {
        var pd = new ProblemDetailsDto { Title = "t", Status = 400, Detail = "d", Instance = "i" };
        pd.Extensions["errors"] = new System.Collections.Generic.Dictionary<string, string[]>
        {
            ["field"] = new[] { "err1" }
        };

        var json = JsonSerializer.Serialize(pd);
        var des = JsonSerializer.Deserialize<ProblemDetailsDto>(json);
        des.Should().NotBeNull();
        // The deserialized Extensions values are JsonElement when using System.Text.Json
        var errorsElement = des!.Extensions["errors"] as System.Text.Json.JsonElement?;
        errorsElement.HasValue.Should().BeTrue();
        var dict = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string[]>>(errorsElement.Value.GetRawText());
        dict.Should().NotBeNull();
        dict.Should().ContainKey("field");
    }
}
