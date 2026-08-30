namespace PlayPredict.Api.LoginAppearance;

public interface ILoginAppearanceCompanyResolver
{
    Task<int> ResolvePublicCompanyIdAsync(CancellationToken cancellationToken = default);
}
