export interface Competition {
  id: number
  experienceId: number
  name: string
  description: string | null
  sport: string
  isActive: boolean
  createdAtUtc: string
}

export type EditionStatus = 'Draft' | 'Active' | 'Finished' | 'Cancelled'

export interface Edition {
  id: number
  competitionId: number
  name: string
  startDateUtc: string
  endDateUtc: string | null
  status: EditionStatus
  createdAtUtc: string
}

export interface Round {
  id: number
  editionId: number
  name: string
  order: number
  startDateUtc: string | null
  endDateUtc: string | null
}

export type MatchStatus = 'Scheduled' | 'InProgress' | 'Finished' | 'Suspended' | 'Cancelled'

export interface Match {
  id: number
  roundId: number
  participantHome: string
  participantAway: string
  startsAtUtc: string
  status: MatchStatus
  homeGoals: number | null
  awayGoals: number | null
  createdAtUtc: string
}

export const EDITION_STATUSES: EditionStatus[] = ['Draft', 'Active', 'Finished', 'Cancelled']
export const MATCH_STATUSES: MatchStatus[] = ['Scheduled', 'InProgress', 'Suspended', 'Cancelled']

export const EDITION_STATUS_LABELS: Record<EditionStatus, string> = {
  Draft: 'Borrador',
  Active: 'Activa',
  Finished: 'Finalizada',
  Cancelled: 'Cancelada',
}

export const MATCH_STATUS_LABELS: Record<MatchStatus, string> = {
  Scheduled: 'Programado',
  InProgress: 'En curso',
  Finished: 'Finalizado',
  Suspended: 'Suspendido',
  Cancelled: 'Cancelado',
}

export interface User {
  id: number
  companyId: number
  firstName: string
  lastName: string
  email: string
  isActive: boolean
  createdAtUtc: string
  lastAccessUtc: string | null
  roles: string[]
}

export interface AuthResponse {
  token: string
  user: User
}

export type EvaluationType = 'ExactScore' | 'CorrectOutcome' | 'Incorrect'

export interface Prediction {
  id: number
  matchId: number
  userId: number
  predictedHomeScore: number
  predictedAwayScore: number
  createdAtUtc: string
  updatedAtUtc: string
  points: number | null
  evaluationType: EvaluationType | null
  evaluationLabel: string | null
  officialHomeScore: number | null
  officialAwayScore: number | null
}

export interface EditionScoringConfiguration {
  id: number
  editionId: number
  exactScorePoints: number
  correctOutcomePoints: number
  incorrectPoints: number
  useExperienceDefaults: boolean
  effectiveExactScorePoints: number
  effectiveCorrectOutcomePoints: number
  effectiveIncorrectPoints: number
  createdAtUtc: string
  updatedAtUtc: string
}

export type ExperienceStatus = 'Draft' | 'Published' | 'Archived'

export const EXPERIENCE_STATUS_LABELS: Record<ExperienceStatus, string> = {
  Draft: 'Borrador',
  Published: 'Publicada',
  Archived: 'Archivada',
}

export interface Experience {
  id: number
  name: string
  description: string | null
  status: ExperienceStatus
  statusLabel: string
  primaryColor: string | null
  secondaryColor: string | null
  logoUrl: string | null
  isPublic: boolean
  defaultExactScorePoints: number
  defaultCorrectOutcomePoints: number
  defaultIncorrectPoints: number
  createdAtUtc: string
  updatedAtUtc: string
}

export interface MatchWithPrediction {
  id: number
  roundId: number
  participantHome: string
  participantAway: string
  startsAtUtc: string
  status: MatchStatus
  homeGoals: number | null
  awayGoals: number | null
  myPrediction: Prediction | null
  canPredict: boolean
}

export interface RankingEntry {
  position: number
  userId: number
  firstName: string
  lastName: string
  points: number
  exactCount: number
  correctCount: number
  incorrectCount: number
  evaluatedCount: number
}

export type PrizeType = 'Money' | 'Product' | 'Service' | 'Coupon' | 'Ticket' | 'Recognition' | 'Other'
export type PrizeScopeType = 'Edition' | 'Round' | 'Special'
export type PrizeAwardCriteria = 'Position' | 'RoundWinner' | 'MostExactScores'
export type PrizeStatus = 'Draft' | 'Published' | 'Closed' | 'Cancelled'

export const PRIZE_TYPES: PrizeType[] = [
  'Money',
  'Product',
  'Service',
  'Coupon',
  'Ticket',
  'Recognition',
  'Other',
]
export const PRIZE_SCOPE_TYPES: PrizeScopeType[] = ['Edition', 'Round', 'Special']
export const PRIZE_AWARD_CRITERIA: PrizeAwardCriteria[] = ['Position', 'RoundWinner', 'MostExactScores']
export const PRIZE_STATUSES: PrizeStatus[] = ['Draft', 'Published', 'Closed', 'Cancelled']

export const PRIZE_TYPE_LABELS: Record<PrizeType, string> = {
  Money: 'Dinero',
  Product: 'Producto',
  Service: 'Servicio',
  Coupon: 'Cupón',
  Ticket: 'Entrada',
  Recognition: 'Reconocimiento',
  Other: 'Otro',
}

export const PRIZE_SCOPE_LABELS: Record<PrizeScopeType, string> = {
  Edition: 'Edición',
  Round: 'Fecha',
  Special: 'Especial',
}

export const PRIZE_CRITERIA_LABELS: Record<PrizeAwardCriteria, string> = {
  Position: 'Posición en el Ranking',
  RoundWinner: 'Ganador de la Fecha',
  MostExactScores: 'Mayor cantidad de marcadores exactos',
}

export const PRIZE_STATUS_LABELS: Record<PrizeStatus, string> = {
  Draft: 'Borrador',
  Published: 'Publicado',
  Closed: 'Cerrado',
  Cancelled: 'Cancelado',
}

export interface PrizeWinnerUser {
  userId: number
  firstName: string
  lastName: string
}

export interface Prize {
  id: number
  editionId: number
  editionName: string
  roundId: number | null
  roundName: string | null
  name: string
  description: string | null
  prizeType: PrizeType
  prizeTypeLabel: string
  referenceValue: string | null
  sponsorName: string | null
  imageUrl: string | null
  scopeType: PrizeScopeType
  scopeLabel: string
  awardCriteria: PrizeAwardCriteria
  criteriaLabel: string
  positionFrom: number | null
  positionTo: number | null
  status: PrizeStatus
  statusLabel: string
  forLabel: string
  currentWinners: PrizeWinnerUser[]
  hasProvisionalWinner: boolean
  createdAtUtc: string
  updatedAtUtc: string
}
