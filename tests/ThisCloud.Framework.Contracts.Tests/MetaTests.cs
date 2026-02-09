using System;
using System.Text.Json;
using FluentAssertions;
using ThisCloud.Framework.Contracts.Web;
using Xunit;

namespace ThisCloud.Framework.Contracts.Tests;

public class MetaTests
{
    [Fact]
    public void Meta_properties_roundtrip_and_values()
    {
        var now = DateTime.UtcNow;
        var meta = new Meta("svc", "v1", now, Guid.NewGuid(), Guid.NewGuid(), "traceid");

        meta.Service.Should().Be("svc");
        meta.Version.Should().Be("v1");
        meta.TimestampUtc.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
        meta.TraceId.Should().Be("traceid");

        var json = JsonSerializer.Serialize(meta);
        var des = JsonSerializer.Deserialize<Meta>(json);
        des.Should().NotBeNull();
        des!.Service.Should().Be(meta.Service);
    }
}
