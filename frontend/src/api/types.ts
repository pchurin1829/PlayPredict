export interface Competition {
  id: number
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
