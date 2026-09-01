using PlayPredict.Api.Data;
using Xunit;

namespace PlayPredict.Api.Tests;

public class LoadTestSeederSafetyTests
{
    private const string LoadTestConnection =
        "Host=db-loadtest;Port=5432;Database=playpredict_loadtest;Username=test;Password=test";

    [Fact]
    public void RequiresExplicitEnablement()
    {
        var options = new LoadTestSeedOptions
        {
            Enabled = false,
            UserCount = 100,
            UserPassword = "loadtest-password"
        };
        var error = Assert.Throws<InvalidOperationException>(() =>
            LoadTestSeeder.ValidateSafety(options, LoadTestSeeder.EnvironmentName, LoadTestConnection));
        Assert.Contains("disabled", error.Message);
    }

    [Fact]
    public void RequiresLoadTestEnvironment()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            LoadTestSeeder.ValidateSafety(ValidOptions(), "Development", LoadTestConnection));
        Assert.Contains("ASPNETCORE_ENVIRONMENT=LoadTest", error.Message);
    }

    [Fact]
    public void RefusesDatabaseWithoutLoadTestInName()
    {
        var normalDatabase = "Host=db;Database=playpredict_db;Username=test;Password=test";
        var error = Assert.Throws<InvalidOperationException>(() =>
            LoadTestSeeder.ValidateSafety(ValidOptions(), LoadTestSeeder.EnvironmentName, normalDatabase));
        Assert.Contains("must contain 'loadtest'", error.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10001)]
    public void RestrictsUserCount(int count)
    {
        var options = new LoadTestSeedOptions
        {
            Enabled = true,
            UserCount = count,
            UserPassword = "loadtest-password"
        };
        Assert.Throws<InvalidOperationException>(() =>
            LoadTestSeeder.ValidateSafety(options, LoadTestSeeder.EnvironmentName, LoadTestConnection));
    }

    [Fact]
    public void AcceptsValidLoadTestConfiguration()
    {
        LoadTestSeeder.ValidateSafety(ValidOptions(), LoadTestSeeder.EnvironmentName, LoadTestConnection);
    }

    [Theory]
    [InlineData(1, "loadtest00001@playpredict.test")]
    [InlineData(10000, "loadtest10000@playpredict.test")]
    public void UserEmailsAreDeterministic(int index, string expected)
    {
        Assert.Equal(expected, LoadTestSeeder.UserEmail(index));
    }

    private static LoadTestSeedOptions ValidOptions() => new()
    {
        Enabled = true,
        UserCount = 100,
        UserPassword = "loadtest-password"
    };
}
