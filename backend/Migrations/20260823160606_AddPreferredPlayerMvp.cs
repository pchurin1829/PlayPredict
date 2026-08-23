using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PlayPredict.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPreferredPlayerMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PreferredPlayerId",
                table: "Predictions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreferredPlayerPoints",
                table: "PredictionEvaluations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ResultPoints",
                table: "PredictionEvaluations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE \"PredictionEvaluations\" SET \"ResultPoints\" = \"Points\"");

            migrationBuilder.AddColumn<bool>(
                name: "PreferredPlayerEnabled",
                table: "EditionScoringConfigurations",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "PreferredPlayerPointsPerGoal",
                table: "EditionScoringConfigurations",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.CreateTable(
                name: "TeamPlayers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ShirtNumber = table.Column<int>(type: "integer", nullable: true),
                    Position = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    PhotoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamPlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamPlayers_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatchScorers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatchId = table.Column<int>(type: "integer", nullable: false),
                    TeamPlayerId = table.Column<int>(type: "integer", nullable: false),
                    Goals = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchScorers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchScorers_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchScorers_TeamPlayers_TeamPlayerId",
                        column: x => x.TeamPlayerId,
                        principalTable: "TeamPlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_PreferredPlayerId",
                table: "Predictions",
                column: "PreferredPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchScorers_MatchId_TeamPlayerId",
                table: "MatchScorers",
                columns: new[] { "MatchId", "TeamPlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchScorers_TeamPlayerId",
                table: "MatchScorers",
                column: "TeamPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamPlayers_TeamId_DisplayName",
                table: "TeamPlayers",
                columns: new[] { "TeamId", "DisplayName" });

            migrationBuilder.AddForeignKey(
                name: "FK_Predictions_TeamPlayers_PreferredPlayerId",
                table: "Predictions",
                column: "PreferredPlayerId",
                principalTable: "TeamPlayers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Predictions_TeamPlayers_PreferredPlayerId",
                table: "Predictions");

            migrationBuilder.DropTable(
                name: "MatchScorers");

            migrationBuilder.DropTable(
                name: "TeamPlayers");

            migrationBuilder.DropIndex(
                name: "IX_Predictions_PreferredPlayerId",
                table: "Predictions");

            migrationBuilder.DropColumn(
                name: "PreferredPlayerId",
                table: "Predictions");

            migrationBuilder.DropColumn(
                name: "PreferredPlayerPoints",
                table: "PredictionEvaluations");

            migrationBuilder.DropColumn(
                name: "ResultPoints",
                table: "PredictionEvaluations");

            migrationBuilder.DropColumn(
                name: "PreferredPlayerEnabled",
                table: "EditionScoringConfigurations");

            migrationBuilder.DropColumn(
                name: "PreferredPlayerPointsPerGoal",
                table: "EditionScoringConfigurations");
        }
    }
}
