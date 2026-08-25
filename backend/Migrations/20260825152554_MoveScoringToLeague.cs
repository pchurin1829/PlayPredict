using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlayPredict.Api.Migrations
{
    /// <inheritdoc />
    public partial class MoveScoringToLeague : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CorrectOutcomePoints",
                table: "Leagues",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<int>(
                name: "ExactScorePoints",
                table: "Leagues",
                type: "integer",
                nullable: false,
                defaultValue: 6);

            migrationBuilder.AddColumn<int>(
                name: "IncorrectPoints",
                table: "Leagues",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "PreferredPlayerEnabled",
                table: "Leagues",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "PreferredPlayerPointsPerGoal",
                table: "Leagues",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "PreferredPlayerPositions",
                table: "Leagues",
                type: "integer",
                nullable: false,
                defaultValue: 12);

            migrationBuilder.AddColumn<bool>(
                name: "UseGeneralScoring",
                table: "Leagues",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "GeneralCorrectOutcomePoints",
                table: "Companies",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<int>(
                name: "GeneralExactScorePoints",
                table: "Companies",
                type: "integer",
                nullable: false,
                defaultValue: 6);

            migrationBuilder.AddColumn<int>(
                name: "GeneralIncorrectPoints",
                table: "Companies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "GeneralPreferredPlayerEnabled",
                table: "Companies",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "GeneralPreferredPlayerPointsPerGoal",
                table: "Companies",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "GeneralPreferredPlayerPositions",
                table: "Companies",
                type: "integer",
                nullable: false,
                defaultValue: 12);

            migrationBuilder.Sql("""
                DELETE FROM "PredictionEvaluations";
                DELETE FROM "Predictions";
                DELETE FROM "LeagueParticipants";
                DELETE FROM "Leagues";
                DELETE FROM "Prizes" WHERE "EditionId" IN (SELECT "Id" FROM "Editions" WHERE "CompetitionId" IN (SELECT "Id" FROM "Competitions" WHERE "Name" IN ('COPA EL NENE - Suc La Plata','Copa EL NENE')));
                DELETE FROM "MatchScorers" WHERE "MatchId" IN (SELECT m."Id" FROM "Matches" m JOIN "Rounds" r ON r."Id"=m."RoundId" JOIN "Editions" e ON e."Id"=r."EditionId" JOIN "Competitions" c ON c."Id"=e."CompetitionId" WHERE c."Name" IN ('COPA EL NENE - Suc La Plata','Copa EL NENE'));
                DELETE FROM "Matches" WHERE "RoundId" IN (SELECT r."Id" FROM "Rounds" r JOIN "Editions" e ON e."Id"=r."EditionId" JOIN "Competitions" c ON c."Id"=e."CompetitionId" WHERE c."Name" IN ('COPA EL NENE - Suc La Plata','Copa EL NENE'));
                DELETE FROM "Rounds" WHERE "EditionId" IN (SELECT e."Id" FROM "Editions" e JOIN "Competitions" c ON c."Id"=e."CompetitionId" WHERE c."Name" IN ('COPA EL NENE - Suc La Plata','Copa EL NENE'));
                DELETE FROM "EditionScoringConfigurations" WHERE "EditionId" IN (SELECT e."Id" FROM "Editions" e JOIN "Competitions" c ON c."Id"=e."CompetitionId" WHERE c."Name" IN ('COPA EL NENE - Suc La Plata','Copa EL NENE'));
                DELETE FROM "Editions" WHERE "CompetitionId" IN (SELECT "Id" FROM "Competitions" WHERE "Name" IN ('COPA EL NENE - Suc La Plata','Copa EL NENE'));
                DELETE FROM "Competitions" WHERE "Name" IN ('COPA EL NENE - Suc La Plata','Copa EL NENE','COPA EL NENE 3');
                DELETE FROM "TeamPlayers" WHERE "TeamId" IN (SELECT "Id" FROM "Teams" WHERE "Name" LIKE 'Equipo %');
                DELETE FROM "Teams" WHERE "Name" LIKE 'Equipo %';
                UPDATE "Competitions" SET "Name"='Liga Profesional de Fútbol' WHERE "Name"='Liga Profesional Argentina';
                UPDATE "Teams" SET "LogoUrl"=NULL WHERE "LogoUrl"='/assets/teams/demo-club.svg';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorrectOutcomePoints",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "ExactScorePoints",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "IncorrectPoints",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "PreferredPlayerEnabled",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "PreferredPlayerPointsPerGoal",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "PreferredPlayerPositions",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "UseGeneralScoring",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "GeneralCorrectOutcomePoints",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "GeneralExactScorePoints",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "GeneralIncorrectPoints",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "GeneralPreferredPlayerEnabled",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "GeneralPreferredPlayerPointsPerGoal",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "GeneralPreferredPlayerPositions",
                table: "Companies");
        }
    }
}
