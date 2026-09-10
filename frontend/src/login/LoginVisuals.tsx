import type { PublicLoginAppearance } from '../api/types'

export function LoginCampaign({ imageUrl }: { imageUrl: string }) {
  return (
    <div className="pp-login__campaign">
      <img src={imageUrl} alt="Campaña promocional" />
    </div>
  )
}

export function LoginAdsColumn({ appearance }: { appearance: PublicLoginAppearance }) {
  const ads = [appearance.adTop, appearance.adMiddle, appearance.adBottom]

  return (
    <aside className="pp-login__ads" aria-label="Ofertas de Supermercados El Nene">
      {ads.map((ad, index) => (
        <div className="pp-login__ad" key={index}>
          <img src={ad.imageUrl} alt={`Publicidad destacada ${index + 1}`} />
        </div>
      ))}
    </aside>
  )
}
