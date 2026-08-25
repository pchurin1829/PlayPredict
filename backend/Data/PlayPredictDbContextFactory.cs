using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PlayPredict.Api.Data;

public sealed class PlayPredictDbContextFactory : IDesignTimeDbContextFactory<PlayPredictDbContext>
{
    public PlayPredictDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5436;Database=playpredict_db;Username=playpredict_user;Password=playpredict_password";
        var options = new DbContextOptionsBuilder<PlayPredictDbContext>().UseNpgsql(connection).Options;
        return new PlayPredictDbContext(options);
    }
}
