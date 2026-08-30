using Microsoft.EntityFrameworkCore;
using Npgsql;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Enums;
using Entities = PlayPredict.Api.Domain.Entities;

namespace PlayPredict.Api.WelcomeCampaigns;

public sealed class WelcomeCampaignService(PlayPredictDbContext db, IWelcomeCampaignImageStorage storage, TimeProvider timeProvider)
{
    private const int MaximumSlides = 3;
    private const decimal MinimumDurationSeconds = 1.0m;
    private const decimal MaximumDurationSeconds = 10.0m;
    private const decimal DefaultDurationSeconds = 2.0m;
    private const WelcomeCampaignFitMode DefaultFitMode = WelcomeCampaignFitMode.Cover;

    public async Task<IReadOnlyList<WelcomeCampaignDto>> GetAllAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var campaigns = await db.WelcomeCampaigns.AsNoTracking()
            .Include(c => c.Slides)
            .Where(c => c.CompanyId == companyId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        return campaigns.Select(ToDto).ToList();
    }

    public async Task<WelcomeCampaignDto?> GetAsync(int companyId, int campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await FindAsync(companyId, campaignId, cancellationToken);
        return campaign is null ? null : ToDto(campaign);
    }

    public async Task<WelcomeCampaignDto> CreateAsync(int companyId, int? userId, string name, DateTime? validFromUtc, DateTime? validToUtc, CancellationToken cancellationToken = default)
    {
        ValidateNameAndRange(name, validFromUtc, validToUtc);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var campaign = new Entities.WelcomeCampaign
        {
            CompanyId = companyId,
            Name = name.Trim(),
            IsActive = false,
            ValidFromUtc = validFromUtc,
            ValidToUtc = validToUtc,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            CreatedByUserId = userId,
            UpdatedByUserId = userId
        };
        db.WelcomeCampaigns.Add(campaign);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(campaign);
    }

    public async Task<WelcomeCampaignDto?> UpdateAsync(int companyId, int? userId, int campaignId, string name, DateTime? validFromUtc, DateTime? validToUtc, CancellationToken cancellationToken = default)
    {
        var campaign = await FindAsync(companyId, campaignId, cancellationToken);
        if (campaign is null) return null;
        ValidateNameAndRange(name, validFromUtc, validToUtc);
        campaign.Name = name.Trim();
        campaign.ValidFromUtc = validFromUtc;
        campaign.ValidToUtc = validToUtc;
        campaign.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        campaign.UpdatedByUserId = userId;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(campaign);
    }

    public async Task<WelcomeCampaignDto?> ActivateAsync(int companyId, int? userId, int campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await FindAsync(companyId, campaignId, cancellationToken);
        if (campaign is null) return null;
        if (campaign.Slides.Count == 0)
            throw new WelcomeCampaignValidationException("NO_SLIDES", "La campaña necesita al menos una imagen para poder activarse.");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var others = await db.WelcomeCampaigns
            .Where(c => c.CompanyId == companyId && c.IsActive && c.Id != campaignId)
            .ToListAsync(cancellationToken);
        foreach (var other in others)
        {
            other.IsActive = false;
            other.UpdatedAtUtc = now;
        }

        campaign.IsActive = true;
        campaign.UpdatedAtUtc = now;
        campaign.UpdatedByUserId = userId;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsActiveCampaignUniqueViolation(ex))
        {
            throw new WelcomeCampaignConcurrencyException(
                "CONCURRENT_ACTIVATION",
                "Otra campaña fue activada simultáneamente. Actualizá la pantalla e intentá nuevamente.");
        }
        return ToDto(campaign);
    }

    private const string ActiveCampaignPerCompanyIndexName = "IX_WelcomeCampaigns_CompanyId_ActiveOnly";

    private static bool IsActiveCampaignUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg
        && pg.ConstraintName == ActiveCampaignPerCompanyIndexName;

    public async Task<WelcomeCampaignDto?> DeactivateAsync(int companyId, int? userId, int campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await FindAsync(companyId, campaignId, cancellationToken);
        if (campaign is null) return null;
        campaign.IsActive = false;
        campaign.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        campaign.UpdatedByUserId = userId;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(campaign);
    }

    public async Task<bool?> DeleteAsync(int companyId, int campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await FindAsync(companyId, campaignId, cancellationToken);
        if (campaign is null) return null;
        if (campaign.IsActive)
            throw new WelcomeCampaignValidationException("CAMPAIGN_ACTIVE", "Desactivá la campaña antes de eliminarla.");

        foreach (var slide in campaign.Slides)
        {
            try { await storage.DeleteAsync(slide.ImageKey, cancellationToken); }
            catch { /* best-effort cleanup; DB delete still proceeds */ }
        }
        db.WelcomeCampaigns.Remove(campaign);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<WelcomeCampaignSlideDto?> AddSlideAsync(int companyId, int campaignId, WelcomeCampaignImageValidationResult image, CancellationToken cancellationToken = default)
    {
        if (!image.IsValid || image.Content is null || image.Extension is null) throw new ArgumentException("A validated image is required.", nameof(image));
        var campaign = await FindAsync(companyId, campaignId, cancellationToken);
        if (campaign is null) return null;
        if (campaign.Slides.Count >= MaximumSlides)
            throw new WelcomeCampaignValidationException("MAX_SLIDES", "Una campaña admite hasta 3 imágenes.");

        var newKey = await storage.SaveAsync(companyId, campaignId, image.Content, image.Extension, cancellationToken);
        try
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var nextOrder = campaign.Slides.Count == 0 ? 1 : campaign.Slides.Max(s => s.SortOrder) + 1;
            var slide = new Entities.WelcomeCampaignSlide
            {
                CampaignId = campaignId,
                ImageKey = newKey,
                SortOrder = nextOrder,
                DurationSeconds = DefaultDurationSeconds,
                FitMode = DefaultFitMode,
                OriginalWidth = image.Width,
                OriginalHeight = image.Height,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            db.WelcomeCampaignSlides.Add(slide);
            campaign.UpdatedAtUtc = now;
            await db.SaveChangesAsync(cancellationToken);
            return ToSlideDto(slide);
        }
        catch
        {
            await storage.DeleteAsync(newKey, cancellationToken);
            throw;
        }
    }

    public async Task<WelcomeCampaignSlideDto?> ReplaceSlideImageAsync(int companyId, int campaignId, int slideId, WelcomeCampaignImageValidationResult image, CancellationToken cancellationToken = default)
    {
        if (!image.IsValid || image.Content is null || image.Extension is null) throw new ArgumentException("A validated image is required.", nameof(image));
        var slide = await FindSlideAsync(companyId, campaignId, slideId, cancellationToken);
        if (slide is null) return null;

        var newKey = await storage.SaveAsync(companyId, campaignId, image.Content, image.Extension, cancellationToken);
        var oldKey = slide.ImageKey;
        try
        {
            slide.ImageKey = newKey;
            slide.OriginalWidth = image.Width;
            slide.OriginalHeight = image.Height;
            slide.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await storage.DeleteAsync(newKey, cancellationToken);
            throw;
        }
        try { await storage.DeleteAsync(oldKey, cancellationToken); }
        catch { /* DB already points at the new immutable file; stale-file cleanup can be retried later. */ }
        return ToSlideDto(slide);
    }

    public async Task<WelcomeCampaignSlideDto?> UpdateSlideAsync(int companyId, int campaignId, int slideId, decimal durationSeconds, WelcomeCampaignFitMode fitMode, CancellationToken cancellationToken = default)
    {
        if (durationSeconds < MinimumDurationSeconds || durationSeconds > MaximumDurationSeconds)
            throw new WelcomeCampaignValidationException("INVALID_DURATION", "La duración debe estar entre 1 y 10 segundos.");
        var slide = await FindSlideAsync(companyId, campaignId, slideId, cancellationToken);
        if (slide is null) return null;
        slide.DurationSeconds = durationSeconds;
        slide.FitMode = fitMode;
        slide.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(cancellationToken);
        return ToSlideDto(slide);
    }

    public async Task<IReadOnlyList<WelcomeCampaignSlideDto>?> ReorderSlideAsync(int companyId, int campaignId, int slideId, int targetSortOrder, CancellationToken cancellationToken = default)
    {
        var campaign = await FindAsync(companyId, campaignId, cancellationToken);
        if (campaign is null) return null;
        var target = campaign.Slides.FirstOrDefault(s => s.Id == slideId);
        if (target is null) return null;

        var ordered = campaign.Slides.OrderBy(s => s.SortOrder).ThenBy(s => s.Id).ToList();
        ordered.Remove(target);
        var insertAt = Math.Clamp(targetSortOrder - 1, 0, ordered.Count);
        ordered.Insert(insertAt, target);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        for (var i = 0; i < ordered.Count; i++)
        {
            var newOrder = i + 1;
            if (ordered[i].SortOrder != newOrder)
            {
                ordered[i].SortOrder = newOrder;
                ordered[i].UpdatedAtUtc = now;
            }
        }
        await db.SaveChangesAsync(cancellationToken);
        return ordered.Select(ToSlideDto).ToList();
    }

    public async Task<IReadOnlyList<WelcomeCampaignSlideDto>?> DeleteSlideAsync(int companyId, int campaignId, int slideId, CancellationToken cancellationToken = default)
    {
        var campaign = await FindAsync(companyId, campaignId, cancellationToken);
        if (campaign is null) return null;
        var slide = campaign.Slides.FirstOrDefault(s => s.Id == slideId);
        if (slide is null) return null;

        try { await storage.DeleteAsync(slide.ImageKey, cancellationToken); }
        catch { /* best-effort cleanup; DB delete still proceeds */ }

        db.WelcomeCampaignSlides.Remove(slide);
        var remaining = campaign.Slides.Where(s => s.Id != slideId).OrderBy(s => s.SortOrder).ThenBy(s => s.Id).ToList();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        for (var i = 0; i < remaining.Count; i++)
        {
            var newOrder = i + 1;
            if (remaining[i].SortOrder != newOrder)
            {
                remaining[i].SortOrder = newOrder;
                remaining[i].UpdatedAtUtc = now;
            }
        }
        await db.SaveChangesAsync(cancellationToken);
        return remaining.Select(ToSlideDto).ToList();
    }

    public async Task<ActiveWelcomeCampaignDto?> GetActiveForCompanyAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var campaign = await db.WelcomeCampaigns.AsNoTracking()
            .Include(c => c.Slides)
            .Where(c => c.CompanyId == companyId && c.IsActive)
            .FirstOrDefaultAsync(cancellationToken);
        if (campaign is null) return null;

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (campaign.ValidFromUtc is { } from && now < from) return null;
        if (campaign.ValidToUtc is { } to && now > to) return null;
        if (campaign.Slides.Count == 0) return null;

        var slides = campaign.Slides.OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
            .Select(s => new ActiveWelcomeCampaignSlideDto(s.Id, storage.GetPublicUrl(s.ImageKey), s.SortOrder, s.DurationSeconds, s.FitMode.ToString()))
            .ToList();
        if (slides.Count == 0) return null;

        return new ActiveWelcomeCampaignDto(campaign.Id, campaign.Name, slides);
    }

    private static void ValidateNameAndRange(string name, DateTime? validFromUtc, DateTime? validToUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new WelcomeCampaignValidationException("INVALID_NAME", "El nombre de la campaña es obligatorio.");
        if (name.Trim().Length > 150)
            throw new WelcomeCampaignValidationException("INVALID_NAME", "El nombre no puede superar los 150 caracteres.");
        if (validFromUtc is not null && validToUtc is not null && validFromUtc > validToUtc)
            throw new WelcomeCampaignValidationException("INVALID_RANGE", "La fecha \"Desde\" debe ser anterior o igual a la fecha \"Hasta\".");
    }

    private async Task<Entities.WelcomeCampaign?> FindAsync(int companyId, int campaignId, CancellationToken cancellationToken) =>
        await db.WelcomeCampaigns.Include(c => c.Slides)
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.CompanyId == companyId, cancellationToken);

    private async Task<Entities.WelcomeCampaignSlide?> FindSlideAsync(int companyId, int campaignId, int slideId, CancellationToken cancellationToken) =>
        await db.WelcomeCampaignSlides.Include(s => s.Campaign)
            .FirstOrDefaultAsync(s => s.Id == slideId && s.CampaignId == campaignId && s.Campaign.CompanyId == companyId, cancellationToken);

    private WelcomeCampaignDto ToDto(Entities.WelcomeCampaign campaign) => new(
        campaign.Id, campaign.Name, campaign.IsActive, campaign.ValidFromUtc, campaign.ValidToUtc,
        campaign.CreatedAtUtc, campaign.UpdatedAtUtc,
        campaign.Slides.OrderBy(s => s.SortOrder).ThenBy(s => s.Id).Select(ToSlideDto).ToList());

    private WelcomeCampaignSlideDto ToSlideDto(Entities.WelcomeCampaignSlide slide) => new(
        slide.Id, storage.GetPublicUrl(slide.ImageKey), slide.SortOrder, slide.DurationSeconds, slide.FitMode.ToString(),
        slide.OriginalWidth, slide.OriginalHeight, slide.UpdatedAtUtc,
        WelcomeCampaignImageValidator.BuildWarnings(slide.OriginalWidth, slide.OriginalHeight));
}
