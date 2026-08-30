const KEY_PREFIX = 'playpredict_welcome_campaign_seen_'

export function isWelcomeCampaignSeen(campaignId: number): boolean {
  try {
    return sessionStorage.getItem(KEY_PREFIX + campaignId) === '1'
  } catch {
    return false
  }
}

export function markWelcomeCampaignSeen(campaignId: number): void {
  try {
    sessionStorage.setItem(KEY_PREFIX + campaignId, '1')
  } catch {
    // sessionStorage no disponible (modo privado, etc.): no bloquea la reproducción.
  }
}

/**
 * Limpia únicamente las marcas de campañas vistas (prefijo playpredict_welcome_campaign_seen_),
 * sin tocar el resto de sessionStorage. Debe llamarse en logout para que, dentro de la misma
 * pestaña, un nuevo login pueda volver a mostrar la misma campaña.
 */
export function clearWelcomeCampaignSeenForSession(): void {
  try {
    const keysToRemove: string[] = []
    for (let i = 0; i < sessionStorage.length; i++) {
      const key = sessionStorage.key(i)
      if (key?.startsWith(KEY_PREFIX)) keysToRemove.push(key)
    }
    keysToRemove.forEach((key) => sessionStorage.removeItem(key))
  } catch {
    // sessionStorage no disponible (modo privado, etc.): no hay nada que limpiar.
  }
}
