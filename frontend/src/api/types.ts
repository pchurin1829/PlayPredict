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
  homeTeamId: number
  awayTeamId: number
  participantHome: string
  participantAway: string
  homeTeamLogoUrl: string | null
  awayTeamLogoUrl: string | null
  startsAtUtc: string
  status: MatchStatus
  homeGoals: number | null
  awayGoals: number | null
  scorers: MatchScorer[]
  createdAtUtc: string
}

export interface MatchScorer { teamPlayerId:number; playerName:string; teamId:number; goals:number }

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

export interface CompetitionDependencies {
  editions: number
  rounds: number
  matches: number
  leagues: number
  participants: number
  predictions: number
  evaluations: number
  matchScorers: number
  prizes: number
  scoringConfigurations: number
  canDelete: boolean
}

export interface OfficialLeagueDependencies {
  participants: number
  predictions: number
  evaluations: number
  officialResults: number
  canDelete: boolean
}

export interface CompanySettings {
  name: string
  shortName: string
  logoUrl: string | null
  generalExactScorePoints: number
  generalCorrectOutcomePoints: number
  generalIncorrectPoints: number
  generalPreferredPlayerEnabled: boolean
  generalPreferredPlayerPointsPerGoal: number
  generalPreferredPlayerPositions: PlayerPosition[]
}

export type LoginImageFitMode = 'Contain' | 'Cover'
export type LoginImageSlot = 'Main' | 'AdTop' | 'AdMiddle' | 'AdBottom'

export interface LoginAppearanceImage {
  imageUrl: string
  fitMode: LoginImageFitMode
}

export interface PublicLoginAppearance {
  version: string
  main: LoginAppearanceImage
  adTop: LoginAppearanceImage
  adMiddle: LoginAppearanceImage
  adBottom: LoginAppearanceImage
}

export interface LoginAppearanceWarning {
  code: string
  message: string
}

export interface AdminLoginAppearanceSlot {
  slot: LoginImageSlot
  effectiveImageUrl: string
  isDefault: boolean
  fitMode: LoginImageFitMode
  updatedAtUtc: string | null
  originalWidth: number
  originalHeight: number
  aspectRatio: number
  recommendedAspectRatio: number
  warnings: LoginAppearanceWarning[]
}

export type WelcomeCampaignFitMode = 'Contain' | 'Cover'

export interface WelcomeCampaignWarning {
  code: string
  message: string
}

export interface WelcomeCampaignSlide {
  id: number
  imageUrl: string
  sortOrder: number
  durationSeconds: number
  fitMode: WelcomeCampaignFitMode
  originalWidth: number
  originalHeight: number
  updatedAtUtc: string
  warnings: WelcomeCampaignWarning[]
}

export interface WelcomeCampaign {
  id: number
  name: string
  isActive: boolean
  validFromUtc: string | null
  validToUtc: string | null
  createdAtUtc: string
  updatedAtUtc: string
  slides: WelcomeCampaignSlide[]
}

export interface ActiveWelcomeCampaignSlide {
  id: number
  imageUrl: string
  sortOrder: number
  durationSeconds: number
  fitMode: WelcomeCampaignFitMode
}

export interface ActiveWelcomeCampaign {
  campaignId: number
  name: string
  slides: ActiveWelcomeCampaignSlide[]
}

export type EvaluationType = 'ExactScore' | 'CorrectOutcome' | 'Incorrect'

export interface Prediction {
  id: number
  matchId: number
  userId: number
  predictedHomeScore: number
  predictedAwayScore: number
  preferredPlayerId: number | null
  preferredPlayerName: string | null
  createdAtUtc: string
  updatedAtUtc: string
  points: number | null
  resultPoints: number | null
  preferredPlayerPoints: number | null
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
  preferredPlayerEnabled: boolean
  preferredPlayerPointsPerGoal: number
  preferredPlayerPositions: PlayerPosition[]
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
  homeTeamId: number
  awayTeamId: number
  participantHome: string
  participantAway: string
  startsAtUtc: string
  status: MatchStatus
  homeGoals: number | null
  awayGoals: number | null
  homePlayers: AvailablePlayer[]
  awayPlayers: AvailablePlayer[]
  quickPreferredPlayers: AvailablePlayer[]
  preferredPlayerEnabled: boolean
  predictionEligible: boolean
  myPrediction: Prediction | null
  canPredict: boolean
}

export type PlayerPosition = 'Arquero' | 'Defensor' | 'Mediocampista' | 'Delantero'
export const PLAYER_POSITIONS: PlayerPosition[] = ['Arquero', 'Defensor', 'Mediocampista', 'Delantero']

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
  sharedCount: number
  isActiveParticipant: boolean
}

export interface AwardStanding {
  position: number | null
  positionFrom: number
  positionTo: number
  tieBreakPending: boolean
  userId: number
  firstName: string
  lastName: string
  points: number
  exactCount: number
  correctCount: number
  incorrectCount: number
  evaluatedCount: number
  accumulatedScoreError: number
  preferredPlayerPoints: number
  isActiveParticipant: boolean
}

export interface UserLeaguePosition {
  leagueId: number
  leagueName: string
  densePosition: number
  sharedCount: number
  points: number
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

export type LeagueScopeType = 'FullCompetition' | 'RoundRange'

export const LEAGUE_SCOPE_LABELS: Record<LeagueScopeType, string> = {
  FullCompetition: 'Toda la Competencia',
  RoundRange: 'Rango de Fechas',
}

export interface RoundSummary {
  id: number
  name: string
  order: number
}

export type LeagueType = 'Official' | 'Private'

export const LEAGUE_TYPE_LABELS: Record<LeagueType, string> = {
  Official: 'OFICIAL PLAYPREDICT',
  Private: 'LIGA DE AMIGOS',
}

export interface LeagueSummary {
  id: number
  name: string
  description: string | null
  competitionId: number
  competitionName: string
  editionId: number
  editionName: string
  scopeType: LeagueScopeType
  leagueType: LeagueType
  sourceLeagueId: number | null
  sourceLeagueName: string | null
  usesFullSourceScope: boolean
  roundFromId: number | null
  roundToId: number | null
  roundFromName: string | null
  roundToName: string | null
  createdByUserId: number
  isCreator: boolean
  participantsCount: number
  isActive: boolean
  inviteCode: string | null
  isParticipant: boolean
}

export interface Team {
  id: number
  name: string
  shortName: string
  logoUrl: string | null
  sport: string
  active: boolean
}

export interface TeamPlayer { id:number; teamId:number; firstName:string; lastName:string; displayName:string; shirtNumber:number|null; position:string|null; active:boolean; photoUrl:string|null }

export type ImportPreviewClassification =
  | 'TeamNew' | 'TeamUnchanged' | 'TeamUpdatable' | 'TeamSportConflict' | 'TeamAmbiguousConflict'
  | 'PlayerNew' | 'PlayerUnchanged' | 'PlayerUpdatable' | 'PlayerAmbiguousConflict'
  | 'UnresolvedTeamError' | 'StructuralError'

export interface ImportIssue { code:string; message:string; sheetName:string|null; rowNumber:number|null; columnName:string|null }
export interface ImportChange { field:string; currentValue:string|null; proposedValue:string|null }
export interface TeamImportPreviewRow { sheet:string; rowNumber:number; entity:string; classification:ImportPreviewClassification; message:string; name:string; shortName:string; sport:string; teamId:number|null; proposedChanges:ImportChange[] }
export interface RosterImportPreviewRow { sheet:string; rowNumber:number; entity:string; classification:ImportPreviewClassification; message:string; clubName:string; firstName:string; lastName:string; displayName:string; position:string|null; teamId:number|null; teamPlayerId:number|null; proposedChanges:ImportChange[] }
export interface TeamImportPreviewSummary { total:number; new:number; unchanged:number; updatable:number; conflicts:number; errors:number }
export interface RosterImportPreviewSummary { total:number; new:number; updatable:number; unchanged:number; conflicts:number; errors:number }
export interface TeamRosterImportPreviewResponse { hash:string; sport:string; teamsSummary:TeamImportPreviewSummary; rostersSummary:RosterImportPreviewSummary; teams:TeamImportPreviewRow[]; rosters:RosterImportPreviewRow[]; issues:ImportIssue[]; canConfirm:boolean }
export interface ImportConfirmationSummary { created:number; updated:number; unchanged:number }
export interface TeamRosterImportConfirmationResponse { status:'Success'|'Rejected'|'Failed'; processedHash:string; message:string; teams:ImportConfirmationSummary; rosters:ImportConfirmationSummary; issues:ImportIssue[] }

export interface UserTeamPreferredPlayer {
  id: number
  teamId: number
  teamName: string
  teamPlayerId: number
  teamPlayerName: string
  isValid: boolean
  createdAtUtc: string
  updatedAtUtc: string
}

export interface PreferredPlayerProfileTeam {
  teamId: number
  teamName: string
  teamShortName: string
  players: Array<{ id: number; name: string }>
  preference: UserTeamPreferredPlayer | null
}
export interface AvailablePlayer { id:number; teamId:number; firstName:string; lastName:string; nickname:string|null; shirtNumber:number|null; position:PlayerPosition }

export interface AdminOfficialLeague {
  id: number
  name: string
  description: string | null
  competitionId: number
  competitionName: string
  editionId: number
  editionName: string
  scopeType: LeagueScopeType
  roundFromId: number | null
  roundToId: number | null
  roundFromName: string | null
  roundToName: string | null
  isActive: boolean
  participantsCount: number
  roundsCount: number
  matchesCount: number
  useGeneralScoring: boolean
  exactScorePoints: number
  correctOutcomePoints: number
  incorrectPoints: number
  preferredPlayerEnabled: boolean
  preferredPlayerPointsPerGoal: number
  preferredPlayerPositions: PlayerPosition[]
  effectiveExactScorePoints: number
  effectiveCorrectOutcomePoints: number
  effectiveIncorrectPoints: number
  effectivePreferredPlayerEnabled: boolean
  effectivePreferredPlayerPointsPerGoal: number
  effectivePreferredPlayerPositions: PlayerPosition[]
  createdAtUtc: string
  updatedAtUtc: string
}

export interface LeagueDetail extends LeagueSummary {
  createdByName: string
  rounds: RoundSummary[]
}

export interface LeagueParticipantInfo {
  userId: number
  firstName: string
  lastName: string
  joinedAtUtc: string
  isCreator: boolean
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
