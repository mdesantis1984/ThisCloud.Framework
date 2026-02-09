using FluentAssertions;
using System.Text.Json;
using ThisCloud.Framework.Contracts.Web;
using ThisCloud.Framework.Contracts.Exceptions;
using Xunit;

namespace ThisCloud.Framework.Contracts.Tests;

public class CoverageExtraTests
{
    [Fact]
    public void ErrorItem_DefaultsAndExtensions_Work()
    {
        var e = new ErrorItem { Type = "t", Title = "tt", Status = 500, Detail = "d", Instance = "i" };
        e.Extensions.Should().NotBeNull();
        e.Extensions.Count.Should().Be(0);
        e.Extensions["code"] = "X";
        e.Extensions["num"] = 123;
        e.Extensions["obj"] = null;

        var json = JsonSerializer.Serialize(e);
        var round = JsonSerializer.Deserialize<ErrorItem>(json);
        round.Should().NotBeNull();
        round!.Title.Should().Be("tt");
        round.Extensions.Should().ContainKey("code");
    }

    [Fact]
    public void ApiEnvelope_CanHaveErrors_And_DataNull()
    {
        var meta = new Meta("svc", "v", System.DateTime.UtcNow, System.Guid.NewGuid(), System.Guid.NewGuid(), null);
        var env = new ApiEnvelope<string> { Meta = meta, Data = null, Errors = new[] { new ErrorItem { Title = "err" } } };
        env.Data.Should().BeNull();
        env.Errors.Should().NotBeEmpty();
        env.Errors[0].Title.Should().Be("err");
    }

    [Fact]
    public void Meta_Record_Equality_And_Deconstruct()
    {
        var now = System.DateTime.UtcNow;
        var a = new Meta("s", "1", now, System.Guid.NewGuid(), System.Guid.NewGuid(), "t");
        var b = new Meta(a.Service, a.Version, a.TimestampUtc, a.CorrelationId, a.RequestId, a.TraceId);
        (a == b).Should().BeTrue();
        var (svc, ver, ts, corr, req, trace) = a;
        svc.Should().Be("s");
        trace.Should().Be("t");
    }

    [Fact]
    public void ProblemDetailsDto_Extensions_Serialization_VariousTypes()
    {
        var pd = new ProblemDetailsDto { Title = "T", Detail = "D", Status = 400 };
        pd.Extensions["code"] = "ERR";
        pd.Extensions["list"] = new[] { "a", "b" };
        pd.Extensions["number"] = 7;

        var json = JsonSerializer.Serialize(pd);
        var doc = JsonSerializer.Deserialize<ProblemDetailsDto>(json);
        doc.Should().NotBeNull();
        doc!.Extensions.Should().ContainKey("code");
        // Values may be JsonElement after deserialization to object
        var val = doc.Extensions["code"];
        (val is JsonElement || val is string).Should().BeTrue();
    }

    [Fact]
    public void ValidationException_AllConstructors_Work_WithNullErrors()
    {
        var v = new ValidationException("msg", null);
        v.Code.Should().Be("VALIDATION_ERROR");
        v.Status.Should().Be(400);
        v.ValidationErrors.Should().BeNull();

        var v2 = new ValidationException("m", new System.Collections.Generic.Dictionary<string, string[]?>());
        v2.ValidationErrors.Should().NotBeNull();
    }
}
