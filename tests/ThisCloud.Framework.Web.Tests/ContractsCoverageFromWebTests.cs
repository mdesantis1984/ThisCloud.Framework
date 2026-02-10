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
        json.Should().Contain("websvc");
        json.Should().Contain("Err");
    }

    [Fact]
    public void ProblemDetailsDto_Roundtrip_With_Extensions()
    {
        var pd = new ThisCloud.Framework.Contracts.Web.ProblemDetailsDto { Title = "T", Detail = "D", Status = 502 };
        pd.Extensions["code"] = "UPSTREAM";
        pd.Extensions["errors"] = new[] { "e1", "e2" };

        var json = JsonSerializer.Serialize(pd);
        var round = JsonSerializer.Deserialize<ThisCloud.Framework.Contracts.Web.ProblemDetailsDto>(json);

        round.Should().NotBeNull();
        round!.Status.Should().Be(502);
        round.Extensions.Should().ContainKey("code");
    }

    [Fact]
    public void ValidationErrors_From_Creates_Dictionary()
    {
        var dict = ThisCloud.Framework.Contracts.Exceptions.ValidationErrors.From(("field1", new[] { "m1", "m2" }));

        dict.Should().NotBeNull();
        dict.Should().ContainKey("field1");
        dict["field1"].Should().Contain("m1");
    }

    [Fact]
    public void Meta_DefaultCtor_And_ErrorItem_AddExtension_Works()
    {
        var m = new ThisCloud.Framework.Contracts.Web.Meta("svc-default", "0.0");
        m.Service.Should().NotBeNull();
        m.Version.Should().NotBeNull();
        m.CorrelationId.Should().NotBe(System.Guid.Empty);

        var ei = new ThisCloud.Framework.Contracts.Web.ErrorItem { Title = "t" };
        ei.AddExtension("k", "v");
        ei.Extensions.Should().ContainKey("k");

        var pd = new ThisCloud.Framework.Contracts.Web.ProblemDetailsDto { Title = "P" };
        pd.Extensions.Should().NotBeNull();
    }

    [Fact]
    public void Meta_Serialization()
    {
        var timestamp = System.DateTimeOffset.UtcNow;
        var corr = System.Guid.NewGuid();
        var req = System.Guid.NewGuid();
        var meta = new Meta("svc-x", "2.3", timestamp, corr, req, "trace-123");

        meta.Service.Should().Be("svc-x");
        meta.Version.Should().Be("2.3");
        meta.CorrelationId.Should().Be(corr);
        meta.RequestId.Should().Be(req);
        meta.TraceId.Should().Be("trace-123");

        var json = JsonSerializer.Serialize(meta);
        json.Should().Contain("svc-x");
        json.Should().Contain("trace-123");
    }

    [Fact]
    public void ApiEnvelope_String_Serialization()
    {
        var meta = new Meta("svc-a", "0.1");
        var env = new ApiEnvelope<string> { Meta = meta, Data = "payload" };

        env.Data.Should().Be("payload");
        env.Meta.Service.Should().Be("svc-a");

        var json = JsonSerializer.Serialize(env);
        json.Should().Contain("payload");
        json.Should().Contain("svc-a");
    }

    [Fact]
    public void ApiEnvelope_Object_WithErrors_Serialization()
    {
        var meta = new Meta("svc-b", "0.2");
        var err = new ErrorItem { Type = "t", Title = "T", Status = 400, Detail = "bad" };
        err.Extensions["field"] = new[] { "f1" };
        var env = new ApiEnvelope<object> { Meta = meta, Data = null, Errors = new System.Collections.Generic.List<ErrorItem> { err } };

        env.Errors.Should().NotBeEmpty();
        env.Errors[0].Title.Should().Be("T");
        env.Errors[0].Extensions.Should().ContainKey("field");
        env.Meta.Service.Should().Be("svc-b");

        var json = JsonSerializer.Serialize(env);
        json.Should().Contain("svc-b");
        json.Should().Contain("\"Title\":\"T\"");
    }

    [Fact]
    public void Exceptions_Constructors_Are_Correct()
    {
        var ve = new ThisCloud.Framework.Contracts.Exceptions.ValidationException("bad", new System.Collections.Generic.Dictionary<string, string[]?> { { "f", new[] { "m" } } });
        ve.Code.Should().Be("VALIDATION_ERROR");
        ve.Status.Should().Be(400);
        ve.ValidationErrors.Should().ContainKey("f");

        var nf = new ThisCloud.Framework.Contracts.Exceptions.NotFoundException("n");
        nf.Code.Should().Be("NOT_FOUND");
        nf.Status.Should().Be(404);
    }
}
