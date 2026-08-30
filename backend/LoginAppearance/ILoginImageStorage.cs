using PlayPredict.Api.Domain.Enums;

namespace PlayPredict.Api.LoginAppearance;

public interface ILoginImageStorage
{
    Task<string> SaveAsync(int companyId, LoginImageSlot slot, byte[] content, string extension, CancellationToken cancellationToken = default);
    Task DeleteAsync(string? imageKey, CancellationToken cancellationToken = default);
    string GetPublicUrl(string imageKey);
}
