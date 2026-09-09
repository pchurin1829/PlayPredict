using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlayPredict.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchScorerOwnGoals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "TeamPlayerId",
                table: "MatchScorers",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<bool>(
                name: "IsOwnGoal",
                table: "MatchScorers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TeamId",
                table: "MatchScorers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Backfill: en filas históricas el equipo beneficiado es el equipo del goleador.
            migrationBuilder.Sql(
                @"UPDATE ""MatchScorers"" AS s SET ""TeamId"" = p.""TeamId"" FROM ""TeamPlayers"" AS p WHERE p.""Id"" = s.""TeamPlayerId"";");

            migrationBuilder.CreateIndex(
                name: "IX_MatchScorers_TeamId",
                table: "MatchScorers",
                column: "TeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_MatchScorers_Teams_TeamId",
                table: "MatchScorers",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MatchScorers_Teams_TeamId",
                table: "MatchScorers");

            migrationBuilder.DropIndex(
                name: "IX_MatchScorers_TeamId",
                table: "MatchScorers");

            migrationBuilder.DropColumn(
                name: "IsOwnGoal",
                table: "MatchScorers");

            migrationBuilder.DropColumn(
                name: "TeamId",
                table: "MatchScorers");

            migrationBuilder.AlterColumn<int>(
                name: "TeamPlayerId",
                table: "MatchScorers",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
