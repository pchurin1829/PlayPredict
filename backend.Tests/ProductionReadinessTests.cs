using System.Net;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Health;
using PlayPredict.Api.Security;
using Xunit;

namespace PlayPredict.Api.Tests;

/// <summary>
/// Cobertura P2.2 local pre-VPS: readiness mínima y configuración de red
/// confiable para ForwardedHeaders (base del rate limiting tras proxy).
/// </summary>
public sealed class ProductionReadinessTests
{
    [Fact]
    public async Task Readiness_is_true_when_database_is_reachable()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<PlayPredictDbContext>()
            .UseSqlite(connection).Options;
        await using var db = new PlayPredictDbContext(options);

        Assert.True(await ReadinessProbe.IsReadyAsync(db));
    }

    [Fact]
    public async Task Readiness_is_false_when_database_is_unreachable()
    {
        var options = new DbContextOptionsBuilder<PlayPredictDbContext>()
            .UseSqlite("Data Source=/nonexistent-dir-xyz/unreachable.db").Options;
        await using var db = new PlayPredictDbContext(options);

        Assert.False(await ReadinessProbe.IsReadyAsync(db));
    }

    [Fact]
    public void Known_networks_parse_valid_cidr_and_ignore_invalid()
    {
        var networks = ForwardedNetworkConfig.ParseNetworks("10.20.0.0/24, not-a-network").ToList();

        var single = Assert.Single(networks);
        Assert.Equal(IPAddress.Parse("10.20.0.0"), single.BaseAddress);
        Assert.Equal(24, single.PrefixLength);
    }

    [Fact]
    public void Known_networks_empty_or_missing_means_loopback_only()
    {
        Assert.Empty(ForwardedNetworkConfig.ParseNetworks(null));
        Assert.Empty(ForwardedNetworkConfig.ParseNetworks(""));
        Assert.Empty(ForwardedNetworkConfig.ParseProxies(null));
    }

    [Fact]
    public void Known_proxies_parse_valid_ip_and_ignore_invalid()
    {
        var proxies = ForwardedNetworkConfig.ParseProxies("10.20.0.5, bogus").ToList();

        Assert.Equal([IPAddress.Parse("10.20.0.5")], proxies);
    }
}
