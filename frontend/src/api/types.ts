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
