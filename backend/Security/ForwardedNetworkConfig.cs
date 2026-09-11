using System.Net;
using Microsoft.AspNetCore.Builder;

namespace PlayPredict.Api.Security;

/// <summary>
/// Red confiable para ForwardedHeaders (P2.2). Solo el reverse proxy de la red
/// interna es fuente válida de X-Forwarded-For/Proto; sin esto el rate limiting
/// por IP no ve la IP real del cliente. Loopback sigue confiable por defecto.
/// Valores inválidos se ignoran (fail-closed hacia loopback).
/// </summary>
public static class ForwardedNetworkConfig
{
    public static void Apply(ForwardedHeadersOptions options, IConfiguration configuration)
    {
        foreach (var network in ParseNetworks(configuration["Forwarded:KnownNetworks"]))
            options.KnownIPNetworks.Add(network);
        foreach (var proxy in ParseProxies(configuration["Forwarded:KnownProxies"]))
            options.KnownProxies.Add(proxy);
    }

    public static IEnumerable<IPNetwork> ParseNetworks(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) yield break;
        foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (IPNetwork.TryParse(part, out var network))
                yield return network;
    }

    public static IEnumerable<IPAddress> ParseProxies(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) yield break;
        foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (IPAddress.TryParse(part, out var address))
                yield return address;
    }
}
