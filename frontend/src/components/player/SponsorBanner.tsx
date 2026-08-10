import ComingSoonBadge from './ComingSoonBadge'
import './SponsorBanner.css'

interface SponsorBannerProps {
  sponsorName?: string | null
  imageUrl?: string | null
}

export default function SponsorBanner({ sponsorName, imageUrl }: SponsorBannerProps) {
  return (
    <div className="sponsor-banner">
      <span className="sponsor-banner__label">Sponsor</span>
      <div className="sponsor-banner__content">
        {imageUrl ? (
          <img src={imageUrl} alt={sponsorName ?? 'Sponsor'} className="sponsor-banner__logo" />
        ) : (
          <div className="sponsor-banner__placeholder">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
              <rect x="3" y="3" width="18" height="18" rx="3" />
              <path d="M8 12h8M12 8v8" />
            </svg>
          </div>
        )}
        <span className="sponsor-banner__name">
          {sponsorName ?? 'Tu marca aquí'}
        </span>
      </div>
      {!sponsorName && <ComingSoonBadge />}
    </div>
  )
}
