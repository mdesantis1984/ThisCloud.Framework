// Copyright (c) 2025 Marco Alejandro De Santis. Licensed under the ISC License.
// See LICENSE file in the project root for full license information.

using ThisCloud.Framework.Loggings.Abstractions;

namespace ThisCloud.Sample.Loggings.MinimalApi.Context;

/// <summary>
/// Sample implementation of ICorrelationContext (Singleton workaround).
/// </summary>
/// <remarks>
/// ⚠️ DEV-ONLY WORKAROUND: Framework DI scope issue (HostBuilderExtensions.cs:87 resolves from root).
/// Framework registers ICorrelationContext as Scoped but resolves during Serilog bootstrap (root scope).
/// This minimal implementation allows the sample to run in Development with Admin.Enabled without modifying src/**.
/// Production deployments should NOT register this workaround - use framework's scoped implementation.
/// TODO: Remove when framework fixes DI scope (change Scoped→Singleton in ServiceCollectionExtensions.cs).
/// </remarks>
internal sealed class SampleCorrelationContext : ICorrelationContext
{
    public Guid? CorrelationId => null;
    public Guid? RequestId => null;
    public string? TraceId => System.Diagnostics.Activity.Current?.TraceId.ToString();
    public string? UserId => null;
}
