using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Domain.Entities;

namespace PlayPredict.Api.Data;

public class PlayPredictDbContext : DbContext
{
    public PlayPredictDbContext(DbContextOptions<PlayPredictDbContext> options)
        : base(options)
    {
    }

    public DbSet<Competition> Competitions => Set<Competition>();
    public DbSet<Edition> Editions => Set<Edition>();
    public DbSet<Round> Rounds => Set<Round>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamPlayer> TeamPlayers => Set<TeamPlayer>();
    public DbSet<MatchScorer> MatchScorers => Set<MatchScorer>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyLoginImageSlot> CompanyLoginImageSlots => Set<CompanyLoginImageSlot>();
    public DbSet<WelcomeCampaign> WelcomeCampaigns => Set<WelcomeCampaign>();
    public DbSet<WelcomeCampaignSlide> WelcomeCampaignSlides => Set<WelcomeCampaignSlide>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Prediction> Predictions => Set<Prediction>();
    public DbSet<EditionScoringConfiguration> EditionScoringConfigurations => Set<EditionScoringConfiguration>();
    public DbSet<PredictionEvaluation> PredictionEvaluations => Set<PredictionEvaluation>();
    public DbSet<Prize> Prizes => Set<Prize>();
    public DbSet<Experience> Experiences => Set<Experience>();
    public DbSet<League> Leagues => Set<League>();
    public DbSet<LeagueParticipant> LeagueParticipants => Set<LeagueParticipant>();
    public DbSet<UserTeamPreferredPlayer> UserTeamPreferredPlayers => Set<UserTeamPreferredPlayer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlayPredictDbContext).Assembly);
    }
}
