using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlayPredict.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchesUniqueRoundHomeAway : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Matches_RoundId_HomeTeamId_AwayTeamId",
                table: "Matches",
                columns: new[] { "RoundId", "HomeTeamId", "AwayTeamId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Matches_RoundId_HomeTeamId_AwayTeamId",
                table: "Matches");
        }
    }
}
