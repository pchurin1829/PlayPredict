using PlayPredict.Api.Dtos;
using PlayPredict.Api.Services;
using Xunit;

namespace PlayPredict.Api.Tests;

public class AwardStandingTests
{
    private static AwardStandingDto Entry(int id, int points, int exact, int correct, int error, int preferred) =>
        new(null, 0, 0, false, id, $"Jugador {id}", "Demo", points, exact, correct, 0, 5, error, preferred, true);

    [Fact]
    public void Automatic_policy_orders_by_points_exact_correct_error_and_preferred_points()
    {
        var result = RankingService.ApplyAwardPolicy([
            Entry(5, 90, 9, 9, 1, 20),
            Entry(4, 90, 9, 9, 1, 10),
            Entry(3, 90, 9, 9, 2, 50),
            Entry(2, 90, 9, 8, 0, 50),
            Entry(1, 90, 8, 20, 0, 50),
            Entry(6, 80, 20, 20, 0, 50),
        ]);

        Assert.Equal([5, 4, 3, 2, 1, 6], result.Select(item => item.UserId));
        Assert.Equal([1, 2, 3, 4, 5, 6], result.Select(item => item.Position));
        Assert.All(result, item => Assert.False(item.TieBreakPending));
    }

    [Fact]
    public void Fully_equal_automatic_criteria_remain_pending_without_name_or_id_tiebreak()
    {
        var result = RankingService.ApplyAwardPolicy([
            Entry(20, 100, 10, 5, 3, 4),
            Entry(10, 100, 10, 5, 3, 4),
            Entry(30, 50, 1, 1, 8, 0),
        ]);

        var pending = result.Where(item => item.Points == 100).ToList();
        Assert.All(pending, item => Assert.True(item.TieBreakPending));
        Assert.All(pending, item => Assert.Null(item.Position));
        Assert.All(pending, item => Assert.Equal((1, 2), (item.PositionFrom, item.PositionTo)));
        Assert.Equal(3, result.Single(item => item.UserId == 30).Position);
    }
}
