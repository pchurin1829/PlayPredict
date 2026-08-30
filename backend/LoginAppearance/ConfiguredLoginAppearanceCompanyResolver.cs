using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PlayPredict.Api.Data;

namespace PlayPredict.Api.LoginAppearance;

public sealed class ConfiguredLoginAppearanceCompanyResolver(
    PlayPredictDbContext db, IOptions<LoginAppearanceOptions> options, IWebHostEnvironment environment)
    : ILoginAppearanceCompanyResolver
{
    public async Task<int> ResolvePublicCompanyIdAsync(CancellationToken cancellationToken = default)
    {
        var configuredId = options.Value.PublicCompanyId;
        if (configuredId is > 0)
        {
            if (await db.Companies.AsNoTracking().AnyAsync(x => x.Id == configuredId && x.IsActive, cancellationToken))
                return configuredId.Value;
            throw new LoginAppearanceConfigurationException("The configured public Company does not exist or is inactive.");
        }

        if (!environment.IsDevelopment())
            throw new LoginAppearanceConfigurationException("LoginAppearance:PublicCompanyId is required outside Development.");

        var candidates = await db.Companies.AsNoTracking().Where(x => x.IsActive).Select(x => x.Id).Take(2).ToListAsync(cancellationToken);
        if (candidates.Count == 1) return candidates[0];
        throw new LoginAppearanceConfigurationException("Development fallback requires exactly one active Company.");
    }
}

public sealed class LoginAppearanceConfigurationException(string message) : Exception(message);
