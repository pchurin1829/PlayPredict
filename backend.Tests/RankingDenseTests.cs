using PlayPredict.Api.Dtos;
using PlayPredict.Api.Services;
using Xunit;

namespace PlayPredict.Api.Tests;

public class RankingDenseTests
{
    private static RankingEntryDto Entry(int userId, int points, int exact = 0, int correct = 0, int incorrect = 0) =>
        new(0, userId, $"Nombre {userId}", "Jugador", points, exact, correct, incorrect, 1, 0, true);

    [Fact]
    public void Dense_ranking_uses_only_distinct_point_totals_for_position()
    {
        var result = RankingService.ApplyDensePositions([
            Entry(1, 100), Entry(2, 100), Entry(3, 50), Entry(4, 23)
        ]);

        Assert.Equal([1, 1, 2, 3], result.Select(entry => entry.Position));
    }

    [Fact]
    public void Dense_ranking_handles_large_shared_positions_without_competition_gaps()
    {
        var ordered = Enumerable.Range(1, 240).Select(id => Entry(id, 100))
            .Concat(Enumerable.Range(241, 12).Select(id => Entry(id, 50)))
            .Concat(Enumerable.Range(253, 400).Select(id => Entry(id, 23)))
            .ToList();

        var result = RankingService.ApplyDensePositions(ordered);

        Assert.Equal([1, 2, 3], result.Select(entry => entry.Position).Distinct());
        Assert.All(result.Where(entry => entry.Points == 100), entry => Assert.Equal(239, entry.SharedCount));
        Assert.All(result.Where(entry => entry.Points == 50), entry => Assert.Equal(11, entry.SharedCount));
        Assert.All(result.Where(entry => entry.Points == 23), entry => Assert.Equal(399, entry.SharedCount));
    }

    [Fact]
    public void Internal_tiebreak_order_does_not_change_visible_position()
    {
        var result = RankingService.ApplyDensePositions([
            Entry(1, 100, exact: 10), Entry(2, 100, exact: 8), Entry(3, 100, exact: 7)
        ]);

        Assert.Equal([1, 2, 3], result.Select(entry => entry.UserId));
        Assert.All(result, entry => Assert.Equal(1, entry.Position));
        Assert.All(result, entry => Assert.Equal(2, entry.SharedCount));
    }

    [Fact]
    public void Shared_count_excludes_the_current_player_and_is_zero_for_a_solo_position()
    {
        var result = RankingService.ApplyDensePositions(
            Enumerable.Range(1, 12).Select(id => Entry(id, 50)).Append(Entry(13, 25)).ToList());

        Assert.Equal(11, result.Single(entry => entry.UserId == 1).SharedCount);
        Assert.Equal(0, result.Single(entry => entry.UserId == 13).SharedCount);
    }
}
