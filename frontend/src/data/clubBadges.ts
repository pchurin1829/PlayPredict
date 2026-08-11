export type BadgePattern = 'solid' | 'halves' | 'sash' | 'stripesV' | 'stripesH'

export interface ClubBadge {
  abbr: string
  pattern: BadgePattern
  primary: string
  secondary: string
  textColor: string
}

// Colores reales de identidad de cada club (no el isotipo oficial) usados
// para armar un escudo genérico por club. Cualquier equipo no listado cae
// al fallback por hash en TeamBadge.
export const CLUB_BADGES: Record<string, ClubBadge> = {
  'Boca Juniors': {
    abbr: 'BOC',
    pattern: 'stripesH',
    primary: '#0a1a4d',
    secondary: '#ffd100',
    textColor: '#ffd100',
  },
  'River Plate': {
    abbr: 'RIV',
    pattern: 'sash',
    primary: '#ffffff',
    secondary: '#d70a20',
    textColor: '#173f8a',
  },
  'Racing Club': {
    abbr: 'RAC',
    pattern: 'stripesV',
    primary: '#7ec8e3',
    secondary: '#ffffff',
    textColor: '#0a3d5c',
  },
  Independiente: {
    abbr: 'IND',
    pattern: 'solid',
    primary: '#d2001f',
    secondary: '#ffffff',
    textColor: '#ffffff',
  },
  Estudiantes: {
    abbr: 'EST',
    pattern: 'halves',
    primary: '#d2001f',
    secondary: '#ffffff',
    textColor: '#ffffff',
  },
  Gimnasia: {
    abbr: 'GIM',
    pattern: 'stripesV',
    primary: '#0f2a5c',
    secondary: '#ffffff',
    textColor: '#ffffff',
  },
  Flamengo: {
    abbr: 'FLA',
    pattern: 'stripesH',
    primary: '#0d0d0d',
    secondary: '#e60023',
    textColor: '#ffffff',
  },
  Palmeiras: {
    abbr: 'PAL',
    pattern: 'solid',
    primary: '#006437',
    secondary: '#ffffff',
    textColor: '#ffffff',
  },
  'Atlético Nacional': {
    abbr: 'ATN',
    pattern: 'stripesV',
    primary: '#046a38',
    secondary: '#ffffff',
    textColor: '#046a38',
  },
  Peñarol: {
    abbr: 'PEN',
    pattern: 'stripesH',
    primary: '#0d0d0d',
    secondary: '#ffd100',
    textColor: '#ffd100',
  },
}
