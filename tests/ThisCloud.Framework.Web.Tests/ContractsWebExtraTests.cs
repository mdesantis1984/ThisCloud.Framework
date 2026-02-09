using FluentAssertions;
using System.Text.Json;
using ThisCloud.Framework.Contracts.Web;
using ThisCloud.Framework.Contracts.Exceptions;
using Xunit;

namespace ThisCloud.Framework.Web.Tests;

public class ContractsWebExtraTests
{
    [Fact]
    public void ErrorItem_SerializationAndExtensions()
    {
        var e = new ErrorItem { Type = "t", Title = "tt", Status = 500, Detail = "d", Instance = "i" };
        e.Extensions["x"] = "v";
        var json = JsonSerializer.Serialize(e);
        var round = JsonSerializer.Deserialize<ErrorItem>(json);
        round.Should().NotBeNull();
        round!.Status.Should().Be(500);
    }

    [Fact]
    public void ApiEnvelope_WithErrorsAndNullData()
    {
        var meta = new Meta("websvc", "1.0", System.DateTime.UtcNow, System.Guid.NewGuid(), System.Guid.NewGuid(), null);
        var env = new ApiEnvelope<string> { Meta = meta, Data = null, Errors = new[] { new ErrorItem { Title = "e" } } };
        env.Errors.Should().NotBeEmpty();
        env.Errors[0].Title.Should().Be("e");
    }

    [Fact]
    public void Meta_Record_BasicAssertions()
    {
        var now = System.DateTime.UtcNow;
        var m = new Meta("s", "v", now, System.Guid.NewGuid(), System.Guid.NewGuid(), "trace");
        m.Service.Should().Be("s");
        m.TraceId.Should().Be("trace");
    }

    [Fact]
    public void ProblemDetailsDto_SerializationVarious()
    {
        var pd = new ProblemDetailsDto { Title = "T", Detail = "D", Status = 400 };
        pd.Extensions["code"] = "ERR";
        pd.Extensions["list"] = new[] { "a" };
        var json = JsonSerializer.Serialize(pd);
        var round = JsonSerializer.Deserialize<ProblemDetailsDto>(json);
        round.Should().NotBeNull();
        round!.Extensions.Should().ContainKey("code");
    }

    [Fact]
    public void Exceptions_ConstructorsAndProperties()
    {
        var v = new ValidationException("msg", new System.Collections.Generic.Dictionary<string, string[]?> { { "f", new[] { "m" } } });
        v.Code.Should().Be("VALIDATION_ERROR");
        v.Status.Should().Be(400);

        var n = new NotFoundException("nf");
        n.Code.Should().Be("NOT_FOUND");
        n.Status.Should().Be(404);

        var c = new ConflictException("conf");
        c.Code.Should().Be("CONFLICT");
        c.Status.Should().Be(409);

        var f = new ForbiddenException("forb");
        f.Code.Should().Be("FORBIDDEN");
        f.Status.Should().Be(403);
    }
}
