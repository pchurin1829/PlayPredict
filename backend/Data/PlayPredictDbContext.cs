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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlayPredictDbContext).Assembly);
    }
}
