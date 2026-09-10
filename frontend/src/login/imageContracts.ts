import type { LoginAppearanceWarning, LoginImageSlot } from '../api/types'

const AD_CONTRACT = {
  width: 600, height: 400, ratio: '3:2', safeArea: 30, recommendedKB: 300,
  formats: 'JPG / PNG / WebP', accept: 'image/jpeg,image/png,image/webp', fit: 'cover' as const,
}

export const LOGIN_IMAGE_CONTRACTS = {
  Main: {
    label: 'Campaña principal',
    hint: 'PNG/WebP con transparencia recomendada. No incluir estadio ni césped: PlayPredict aporta el fondo.',
    width: 800, height: 900, ratio: '8:9', safeArea: 50, recommendedKB: 500,
    formats: 'PNG / WebP', accept: 'image/png,image/webp', fit: 'contain' as const,
  },
  AdTop: { ...AD_CONTRACT, label: 'Publicidad superior', hint: 'Primera publicidad de la columna lateral.' },
  AdMiddle: { ...AD_CONTRACT, label: 'Publicidad media', hint: 'Segunda publicidad de la columna lateral.' },
  AdBottom: { ...AD_CONTRACT, label: 'Publicidad inferior', hint: 'Tercera publicidad de la columna lateral.' },
} satisfies Record<LoginImageSlot, object>

export function getLoginImageWarnings(slot: LoginImageSlot, width: number, height: number, bytes?: number): LoginAppearanceWarning[] {
  const contract = LOGIN_IMAGE_CONTRACTS[slot]
  const warnings: LoginAppearanceWarning[] = []
  if (width < contract.width || height < contract.height) {
    warnings.push({ code: 'LOW_RESOLUTION', message: `Resolución inferior a la recomendada de ${contract.width}×${contract.height} px.` })
  }
  if (Math.abs((width / height) / (contract.width / contract.height) - 1) > 0.05) {
    warnings.push({ code: 'ASPECT_RATIO_MISMATCH', message: `La proporción difiere más de 5% de ${contract.ratio}. ${slot === 'Main' ? 'Se mostrará completa, con espacio alrededor.' : 'Puede recortarse contenido al cubrir el slot.'}` })
  }
  if (bytes !== undefined && bytes > contract.recommendedKB * 1000) {
    warnings.push({ code: 'RECOMMENDED_WEIGHT', message: `Supera el peso recomendado de ${contract.recommendedKB} KB. Es una recomendación, no un bloqueo de subida.` })
  }
  return warnings
}
