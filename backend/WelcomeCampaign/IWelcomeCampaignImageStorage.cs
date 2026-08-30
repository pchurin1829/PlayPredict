namespace PlayPredict.Api.WelcomeCampaigns;

public interface IWelcomeCampaignImageStorage
{
    Task<string> SaveAsync(int companyId, int campaignId, byte[] content, string extension, CancellationToken cancellationToken = default);
    Task DeleteAsync(string? imageKey, CancellationToken cancellationToken = default);
    string GetPublicUrl(string imageKey);
}
