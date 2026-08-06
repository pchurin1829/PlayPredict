using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PlayPredict.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLeagues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 0) Renombra el Rol existente "USER" a "PLAYER" (Sprint 8.5, decisión 1) en el
            // lugar, preservando su Id y por lo tanto todas las UserRoles existentes — nunca
            // inserta un Rol "PLAYER" nuevo y vacío, que dejaría a los usuarios existentes
            // apuntando al Rol viejo. Sin efecto si esta base ya no tuviera ningún "USER".
            migrationBuilder.Sql(@"
                UPDATE ""Roles"" SET ""Name"" = 'PLAYER' WHERE ""Name"" = 'USER';
            ");

            // 1) Tablas nuevas primero: hacen falta para poder backfillear Predictions.LeagueId.
            migrationBuilder.CreateTable(
                name: "Leagues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CompetitionId = table.Column<int>(type: "integer", nullable: false),
                    ScopeType = table.Column<int>(type: "integer", nullable: false),
                    RoundFromId = table.Column<int>(type: "integer", nullable: true),
                    RoundToId = table.Column<int>(type: "integer", nullable: true),
                    InviteCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leagues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Leagues_Competitions_CompetitionId",
                        column: x => x.CompetitionId,
                        principalTable: "Competitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Leagues_Rounds_RoundFromId",
                        column: x => x.RoundFromId,
                        principalTable: "Rounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Leagues_Rounds_RoundToId",
                        column: x => x.RoundToId,
                        principalTable: "Rounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Leagues_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Leagues_CompetitionId",
                table: "Leagues",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Leagues_CreatedByUserId",
                table: "Leagues",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Leagues_InviteCode",
                table: "Leagues",
                column: "InviteCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leagues_RoundFromId",
                table: "Leagues",
                column: "RoundFromId");

            migrationBuilder.CreateIndex(
                name: "IX_Leagues_RoundToId",
                table: "Leagues",
                column: "RoundToId");

            migrationBuilder.CreateTable(
                name: "LeagueParticipants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeagueId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    JoinedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeagueParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeagueParticipants_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeagueParticipants_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeagueParticipants_LeagueId_UserId",
                table: "LeagueParticipants",
                columns: new[] { "LeagueId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeagueParticipants_UserId",
                table: "LeagueParticipants",
                column: "UserId");

            // 2) Predictions.LeagueId se agrega NULLABLE primero: hay Predictions existentes
            // (de antes del Sprint 8.5) que todavía no tienen ninguna Liga asignada.
            migrationBuilder.AddColumn<int>(
                name: "LeagueId",
                table: "Predictions",
                type: "integer",
                nullable: true);

            // 3) Backfill 100% dinámico: por cada Competencia que ya tenga Pronósticos, se crea
            // una Liga técnica de migración ("[Migración] Liga general — {Competencia}"),
            // se incorporan como Participantes todos los usuarios que ya pronosticaron en esa
            // Competencia, y se asignan sus Pronósticos existentes a esa Liga. No depende de
            // IDs concretos de usuarios/competencias/ediciones/fechas/partidos: todo se resuelve
            // por JOIN. Es idempotente: si ya existe la Liga de backfill para una Competencia
            // (NOT EXISTS), no la vuelve a crear ni duplica Participantes.
            migrationBuilder.Sql(@"
                INSERT INTO ""Leagues""
                    (""Name"", ""CompetitionId"", ""ScopeType"", ""RoundFromId"", ""RoundToId"",
                     ""InviteCode"", ""IsActive"", ""CreatedByUserId"", ""CreatedAtUtc"", ""UpdatedAtUtc"")
                SELECT
                    '[Migración] Liga general — ' || comp.""Name"",
                    comp.""Id"",
                    0, -- LeagueScopeType.FullCompetition
                    NULL,
                    NULL,
                    'MIG-' || comp.""Id"" || '-' || substr(md5(random()::text || comp.""Id""::text || clock_timestamp()::text), 1, 8),
                    TRUE,
                    (
                        SELECT MIN(p.""UserId"")
                        FROM ""Predictions"" p
                        JOIN ""Matches"" m ON m.""Id"" = p.""MatchId""
                        JOIN ""Rounds"" r ON r.""Id"" = m.""RoundId""
                        JOIN ""Editions"" e ON e.""Id"" = r.""EditionId""
                        WHERE e.""CompetitionId"" = comp.""Id""
                    ),
                    now(),
                    now()
                FROM ""Competitions"" comp
                WHERE EXISTS (
                    SELECT 1
                    FROM ""Predictions"" p
                    JOIN ""Matches"" m ON m.""Id"" = p.""MatchId""
                    JOIN ""Rounds"" r ON r.""Id"" = m.""RoundId""
                    JOIN ""Editions"" e ON e.""Id"" = r.""EditionId""
                    WHERE e.""CompetitionId"" = comp.""Id""
                )
                AND NOT EXISTS (
                    SELECT 1 FROM ""Leagues"" l
                    WHERE l.""CompetitionId"" = comp.""Id""
                      AND l.""Name"" = '[Migración] Liga general — ' || comp.""Name""
                );
            ");

            migrationBuilder.Sql(@"
                INSERT INTO ""LeagueParticipants"" (""LeagueId"", ""UserId"", ""JoinedAtUtc"")
                SELECT DISTINCT l.""Id"", p.""UserId"", now()
                FROM ""Predictions"" p
                JOIN ""Matches"" m ON m.""Id"" = p.""MatchId""
                JOIN ""Rounds"" r ON r.""Id"" = m.""RoundId""
                JOIN ""Editions"" e ON e.""Id"" = r.""EditionId""
                JOIN ""Leagues"" l ON l.""CompetitionId"" = e.""CompetitionId""
                    AND l.""Name"" = '[Migración] Liga general — ' || (SELECT c.""Name"" FROM ""Competitions"" c WHERE c.""Id"" = e.""CompetitionId"")
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""LeagueParticipants"" lp
                    WHERE lp.""LeagueId"" = l.""Id"" AND lp.""UserId"" = p.""UserId""
                );
            ");

            migrationBuilder.Sql(@"
                UPDATE ""Predictions"" p
                SET ""LeagueId"" = l.""Id""
                FROM ""Matches"" m
                JOIN ""Rounds"" r ON r.""Id"" = m.""RoundId""
                JOIN ""Editions"" e ON e.""Id"" = r.""EditionId""
                JOIN ""Leagues"" l ON l.""CompetitionId"" = e.""CompetitionId""
                    AND l.""Name"" = '[Migración] Liga general — ' || (SELECT c.""Name"" FROM ""Competitions"" c WHERE c.""Id"" = e.""CompetitionId"")
                WHERE p.""MatchId"" = m.""Id"" AND p.""LeagueId"" IS NULL;
            ");

            // 4) Comprobación explícita antes de exigir NOT NULL: si quedara algún Pronóstico
            // sin Liga asignada, la migración aborta (transaccional) en vez de perder integridad.
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM ""Predictions"" WHERE ""LeagueId"" IS NULL) THEN
                        RAISE EXCEPTION 'Migración AddLeagues: quedaron Predictions con LeagueId nulo tras el backfill. Abortando.';
                    END IF;
                END $$;
            ");

            migrationBuilder.AlterColumn<int>(
                name: "LeagueId",
                table: "Predictions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            // 5) Identidad lógica definitiva del Pronóstico: LeagueId + UserId + MatchId.
            migrationBuilder.DropIndex(
                name: "IX_Predictions_UserId_MatchId",
                table: "Predictions");

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_LeagueId_UserId_MatchId",
                table: "Predictions",
                columns: new[] { "LeagueId", "UserId", "MatchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_UserId",
                table: "Predictions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Predictions_Leagues_LeagueId",
                table: "Predictions",
                column: "LeagueId",
                principalTable: "Leagues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Predictions_Leagues_LeagueId",
                table: "Predictions");

            migrationBuilder.DropTable(
                name: "LeagueParticipants");

            migrationBuilder.DropTable(
                name: "Leagues");

            migrationBuilder.DropIndex(
                name: "IX_Predictions_LeagueId_UserId_MatchId",
                table: "Predictions");

            migrationBuilder.DropIndex(
                name: "IX_Predictions_UserId",
                table: "Predictions");

            migrationBuilder.DropColumn(
                name: "LeagueId",
                table: "Predictions");

            // Nota: si ya existieran Predictions de más de una Liga para el mismo par
            // (UserId, MatchId) al momento de revertir, esta recreación del índice viejo
            // fallará por duplicados — comportamiento esperado de un rollback tardío.
            migrationBuilder.CreateIndex(
                name: "IX_Predictions_UserId_MatchId",
                table: "Predictions",
                columns: new[] { "UserId", "MatchId" },
                unique: true);

            migrationBuilder.Sql(@"
                UPDATE ""Roles"" SET ""Name"" = 'USER' WHERE ""Name"" = 'PLAYER';
            ");
        }
    }
}
