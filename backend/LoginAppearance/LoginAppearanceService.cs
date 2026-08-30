using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;

namespace PlayPredict.Api.LoginAppearance;

public sealed class LoginAppearanceService(PlayPredictDbContext db, ILoginImageStorage storage, TimeProvider timeProvider)
{
    public async Task<PublicLoginAppearanceDto> GetPublicAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var rows = await db.CompanyLoginImageSlots.AsNoTracking().Where(x => x.CompanyId == companyId).ToListAsync(cancellationToken);
        var map = rows.ToDictionary(x => x.Slot);
        LoginAppearanceImageDto Resolve(LoginImageSlot slot)
        {
            var defaults = LoginAppearanceDefaults.Slots[slot];
            map.TryGetValue(slot, out var row);
            return new(row?.ImageKey is { Length: > 0 } key ? storage.GetPublicUrl(key) : defaults.ImageUrl,
                (row?.FitMode ?? defaults.FitMode).ToString());
        }
        var version = rows.Count == 0 ? "default-v1" : rows.Max(x => x.UpdatedAtUtc).ToUniversalTime().ToString("O");
        return new(version, Resolve(LoginImageSlot.Main), Resolve(LoginImageSlot.AdTop), Resolve(LoginImageSlot.AdMiddle), Resolve(LoginImageSlot.AdBottom));
    }

    public async Task<IReadOnlyList<AdminLoginAppearanceSlotDto>> GetAdminAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var rows = await db.CompanyLoginImageSlots.AsNoTracking().Where(x => x.CompanyId == companyId).ToDictionaryAsync(x => x.Slot, cancellationToken);
        return Enum.GetValues<LoginImageSlot>().Select(slot => ToAdminDto(slot, rows.GetValueOrDefault(slot))).ToList();
    }

    public async Task<AdminLoginAppearanceSlotDto> ReplaceImageAsync(int companyId, int userId, LoginImageSlot slot,
        LoginImageValidationResult image, CancellationToken cancellationToken = default)
    {
        if (!image.IsValid || image.Content is null || image.Extension is null) throw new ArgumentException("A validated image is required.", nameof(image));
        var newKey = await storage.SaveAsync(companyId, slot, image.Content, image.Extension, cancellationToken);
        string? oldKey = null;
        try
        {
            var row = await db.CompanyLoginImageSlots.FindAsync([companyId, slot], cancellationToken);
            if (row is null)
            {
                row = new CompanyLoginImageSlot { CompanyId = companyId, Slot = slot, FitMode = LoginAppearanceDefaults.Slots[slot].FitMode };
                db.CompanyLoginImageSlots.Add(row);
            }
            oldKey = row.ImageKey;
            row.ImageKey = newKey;
            row.OriginalWidth = image.Width;
            row.OriginalHeight = image.Height;
            row.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
            row.UpdatedByUserId = userId;
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await storage.DeleteAsync(newKey, cancellationToken);
            throw;
        }
        if (oldKey is not null)
        {
            try { await storage.DeleteAsync(oldKey, cancellationToken); }
            catch { /* DB already points at the new immutable file; stale-file cleanup can be retried later. */ }
        }
        var saved = await db.CompanyLoginImageSlots.AsNoTracking().SingleAsync(x => x.CompanyId == companyId && x.Slot == slot, cancellationToken);
        return ToAdminDto(slot, saved);
    }

    public async Task<AdminLoginAppearanceSlotDto> UpdateFitModeAsync(int companyId, int userId, LoginImageSlot slot,
        LoginImageFitMode fitMode, CancellationToken cancellationToken = default)
    {
        var row = await db.CompanyLoginImageSlots.FindAsync([companyId, slot], cancellationToken);
        if (row is null)
        {
            row = new CompanyLoginImageSlot { CompanyId = companyId, Slot = slot };
            db.CompanyLoginImageSlots.Add(row);
        }
        row.FitMode = fitMode;
        row.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        row.UpdatedByUserId = userId;
        await db.SaveChangesAsync(cancellationToken);
        return ToAdminDto(slot, row);
    }

    public async Task<AdminLoginAppearanceSlotDto> RestoreDefaultAsync(int companyId, LoginImageSlot slot, CancellationToken cancellationToken = default)
    {
        var row = await db.CompanyLoginImageSlots.FindAsync([companyId, slot], cancellationToken);
        var oldKey = row?.ImageKey;
        if (row is not null)
        {
            db.CompanyLoginImageSlots.Remove(row);
            await db.SaveChangesAsync(cancellationToken);
        }
        if (oldKey is not null)
        {
            try { await storage.DeleteAsync(oldKey, cancellationToken); }
            catch { /* Restoring the DB default has priority over best-effort physical cleanup. */ }
        }
        return ToAdminDto(slot, null);
    }

    private AdminLoginAppearanceSlotDto ToAdminDto(LoginImageSlot slot, CompanyLoginImageSlot? row)
    {
        var defaults = LoginAppearanceDefaults.Slots[slot];
        var isDefault = string.IsNullOrWhiteSpace(row?.ImageKey);
        var width = isDefault ? defaults.Width : row!.OriginalWidth ?? defaults.Width;
        var height = isDefault ? defaults.Height : row!.OriginalHeight ?? defaults.Height;
        return new(slot.ToString(), isDefault ? defaults.ImageUrl : storage.GetPublicUrl(row!.ImageKey!), isDefault,
            (row?.FitMode ?? defaults.FitMode).ToString(), row?.UpdatedAtUtc, width, height,
            Math.Round((double)width / height, 4), Math.Round(LoginAppearanceDefaults.RecommendedAspectRatio, 4),
            LoginImageValidator.BuildWarnings(slot, width, height));
    }
}
