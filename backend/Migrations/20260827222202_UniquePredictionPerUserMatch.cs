using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlayPredict.Api.Migrations
{
    public partial class UniquePredictionPerUserMatch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Primero se conserva el contexto de Liga de cada evaluación.
            migrationBuilder.AddColumn<int>(
                name: "LeagueId", table: "PredictionEvaluations", type: "integer", nullable: true);
            migrationBuilder.Sql("""
                UPDATE "PredictionEvaluations" e
                SET "LeagueId" = p."LeagueId"
                FROM "Predictions" p
                WHERE p."Id" = e."PredictionId";
                """);
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM "PredictionEvaluations" WHERE "LeagueId" IS NULL) THEN
                        RAISE EXCEPTION 'UniquePredictionPerUserMatch: existen evaluaciones sin LeagueId luego del backfill.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AddColumn<DateTime>(
                name: "LeftAtUtc", table: "LeagueParticipants", type: "timestamp with time zone", nullable: true);

            // Un conflicto deportivo nunca se resuelve eligiendo silenciosamente una versión.
            migrationBuilder.Sql("""
                DO $$
                DECLARE conflict_count integer;
                BEGIN
                    SELECT count(*) INTO conflict_count
                    FROM (
                        SELECT "UserId", "MatchId"
                        FROM "Predictions"
                        GROUP BY "UserId", "MatchId"
                        HAVING count(DISTINCT ROW("PredictedHomeScore", "PredictedAwayScore", "PreferredPlayerId")) > 1
                    ) conflicts;
                    IF conflict_count > 0 THEN
                        RAISE EXCEPTION 'UniquePredictionPerUserMatch: % pares UserId+MatchId tienen marcadores o PreferredPlayer conflictivos. La migración se abortó sin borrar datos.', conflict_count;
                    END IF;
                END $$;
                """);

            migrationBuilder.DropIndex(name: "IX_PredictionEvaluations_PredictionId", table: "PredictionEvaluations");

            // Duplicados idénticos: conservar el menor Id y transferir todas sus evaluaciones.
            migrationBuilder.Sql("""
                WITH canonical AS (
                    SELECT "UserId", "MatchId", min("Id") AS "CanonicalId"
                    FROM "Predictions"
                    GROUP BY "UserId", "MatchId"
                )
                UPDATE "PredictionEvaluations" e
                SET "PredictionId" = c."CanonicalId"
                FROM "Predictions" p
                JOIN canonical c ON c."UserId" = p."UserId" AND c."MatchId" = p."MatchId"
                WHERE e."PredictionId" = p."Id" AND p."Id" <> c."CanonicalId";

                WITH canonical AS (
                    SELECT "UserId", "MatchId", min("Id") AS "CanonicalId"
                    FROM "Predictions"
                    GROUP BY "UserId", "MatchId"
                )
                DELETE FROM "Predictions" p
                USING canonical c
                WHERE p."UserId" = c."UserId" AND p."MatchId" = c."MatchId" AND p."Id" <> c."CanonicalId";
                """);

            migrationBuilder.DropForeignKey(name: "FK_Predictions_Leagues_LeagueId", table: "Predictions");
            migrationBuilder.DropIndex(name: "IX_Predictions_LeagueId_UserId_MatchId", table: "Predictions");
            migrationBuilder.DropIndex(name: "IX_Predictions_UserId", table: "Predictions");
            migrationBuilder.DropIndex(name: "IX_LeagueParticipants_LeagueId_UserId", table: "LeagueParticipants");
            migrationBuilder.DropColumn(name: "LeagueId", table: "Predictions");

            migrationBuilder.AlterColumn<int>(
                name: "LeagueId", table: "PredictionEvaluations", type: "integer", nullable: false,
                oldClrType: typeof(int), oldType: "integer", oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_UserId_MatchId", table: "Predictions",
                columns: new[] { "UserId", "MatchId" }, unique: true);
            migrationBuilder.CreateIndex(
                name: "IX_PredictionEvaluations_LeagueId", table: "PredictionEvaluations", column: "LeagueId");
            migrationBuilder.CreateIndex(
                name: "IX_PredictionEvaluations_PredictionId_LeagueId", table: "PredictionEvaluations",
                columns: new[] { "PredictionId", "LeagueId" }, unique: true);
            migrationBuilder.CreateIndex(
                name: "IX_LeagueParticipants_LeagueId_UserId", table: "LeagueParticipants",
                columns: new[] { "LeagueId", "UserId" }, unique: true, filter: "\"LeftAtUtc\" IS NULL");
            migrationBuilder.AddForeignKey(
                name: "FK_PredictionEvaluations_Leagues_LeagueId", table: "PredictionEvaluations", column: "LeagueId",
                principalTable: "Leagues", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder) =>
            throw new NotSupportedException("El downgrade no puede reconstruir una Liga propietaria para un Prediction global sin perder semántica.");
    }
}
