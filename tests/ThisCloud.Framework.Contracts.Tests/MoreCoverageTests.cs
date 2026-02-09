using FluentAssertions;
using System.Text.Json;
using ThisCloud.Framework.Contracts.Web;
using ThisCloud.Framework.Contracts.Exceptions;
using Xunit;

namespace ThisCloud.Framework.Contracts.Tests;

public class MoreCoverageTests
{
    [Fact]
    public void ApiEnvelope_ExercisesAllMembers()
    {
        var meta = new Meta("svc", "v", System.DateTime.UtcNow, System.Guid.NewGuid(), System.Guid.NewGuid(), "trace");
        var err = new ErrorItem { Title = "E", Detail = "d", Status = 500 };
        var env = new ApiEnvelope<ErrorItem> { Meta = meta, Data = err, Errors = new[] { err } };

        env.Meta.Should().BeSameAs(meta);
        env.Data.Should().NotBeNull();
        env.Errors.Should().ContainSingle().Which.Title.Should().Be("E");

        // mutate via serialization roundtrip
        var json = JsonSerializer.Serialize(env);
        var round = JsonSerializer.Deserialize<ApiEnvelope<ErrorItem>>(json);
        round.Should().NotBeNull();
        round!.Errors.Should().NotBeNull();
    }

    [Fact]
    public void ProblemDetails_Extensions_CoverBranches()
    {
        var pd = new ProblemDetailsDto { Title = "T", Status = 422 };
        pd.Extensions["string"] = "s";
        pd.Extensions["number"] = 1;
        pd.Extensions["null"] = null;
        pd.Extensions["array"] = new[] { "a" };

        // Access each extension to ensure getter paths executed
        foreach (var kv in pd.Extensions)
        {
            kv.Key.Should().NotBeNullOrWhiteSpace();
        }

        var json = JsonSerializer.Serialize(pd);
        var round = JsonSerializer.Deserialize<ProblemDetailsDto>(json);
        round.Should().NotBeNull();
    }

    [Fact]
    public void Exceptions_ToString_And_ValidationErrors()
    {
        var ve = new ValidationException("vmsg", new System.Collections.Generic.Dictionary<string, string[]?> { { "f", new[] { "m" } } });
        ve.Message.Should().Be("vmsg");
        ve.ToString().Should().Contain("vmsg");
        ve.ValidationErrors.Should().ContainKey("f");

        var nf = new NotFoundException("nf");
        nf.Message.Should().Be("nf");
        nf.ToString().Should().Contain("nf");

        var cf = new ConflictException("cf");
        cf.ToString().Should().Contain("cf");

        var forb = new ForbiddenException("forb");
        forb.ToString().Should().Contain("forb");
    }

    [Fact]
    public void ThisCloudHeaders_AccessViaReflection()
    {
        var t = typeof(ThisCloudHeaders);
        var fields = t.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        fields.Length.Should().BeGreaterOrEqualTo(2);
        foreach (var f in fields)
        {
            var val = f.GetValue(null) as string;
            val.Should().NotBeNullOrWhiteSpace();
        }
    }
}
