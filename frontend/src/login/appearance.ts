import type { PublicLoginAppearance } from '../api/types'

// Local presentation defaults for the existing Login Appearance API.
// Company-managed images always come from /public/login-appearance when available.
export const LOGIN_APPEARANCE = {
  // Fixed PlayPredict stadium, independent of company-managed campaigns and ads.
  backgroundImageUrl: '/assets/login/login-stadium-background.png' as string | null,
  fallback: {
    version: 'default-v1',
    main: { imageUrl: '/assets/login/login-campaign-copa-el-nene.png', fitMode: 'Contain' },
    adTop: { imageUrl: '/assets/el-nene-login/producto-1.png', fitMode: 'Cover' },
    adMiddle: { imageUrl: '/assets/el-nene-login/producto-2.png', fitMode: 'Cover' },
    adBottom: { imageUrl: '/assets/el-nene-login/producto-3.png', fitMode: 'Cover' },
  } satisfies PublicLoginAppearance,
}

// Migrate only the old bundled campaign; preserve company-uploaded Appearance URLs.
export function resolveLoginCampaignUrl(imageUrl: string): string {
  return imageUrl === '/assets/el-nene-login/copa-el-nene-panel-principal.png'
    ? LOGIN_APPEARANCE.fallback.main.imageUrl
    : imageUrl
}

export function resolveLoginAppearance(appearance: PublicLoginAppearance): PublicLoginAppearance {
  const imageUrl = resolveLoginCampaignUrl(appearance.main.imageUrl)
  if (imageUrl === appearance.main.imageUrl) return appearance
  return {
    ...appearance,
    main: { ...appearance.main, imageUrl },
  }
}
