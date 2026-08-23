using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PlayPredict.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamsAndMatchTeamReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ShortName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Sport = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                });

            migrationBuilder.AddColumn<int>(name: "AwayTeamId", table: "Matches", type: "integer", nullable: true);
            migrationBuilder.AddColumn<int>(name: "HomeTeamId", table: "Matches", type: "integer", nullable: true);

            // Conserva todos los nombres históricos: crea un Team por cada participante
            // ya utilizado y vincula los partidos antes de volver obligatorias las FK.
            migrationBuilder.Sql("""
                INSERT INTO "Teams" ("Name", "ShortName", "Sport", "Active")
                SELECT names."Name", LEFT(names."Name", 50), 'Fútbol', TRUE
                FROM (
                    SELECT DISTINCT "ParticipantHome" AS "Name" FROM "Matches"
                    UNION
                    SELECT DISTINCT "ParticipantAway" AS "Name" FROM "Matches"
                ) names
                WHERE names."Name" IS NOT NULL AND BTRIM(names."Name") <> '';

                UPDATE "Matches" m SET "HomeTeamId" = t."Id" FROM "Teams" t
                WHERE t."Name" = m."ParticipantHome";
                UPDATE "Matches" m SET "AwayTeamId" = t."Id" FROM "Teams" t
                WHERE t."Name" = m."ParticipantAway";
                """);

            migrationBuilder.AlterColumn<int>(name: "AwayTeamId", table: "Matches", type: "integer", nullable: false, oldClrType: typeof(int), oldType: "integer", oldNullable: true);
            migrationBuilder.AlterColumn<int>(name: "HomeTeamId", table: "Matches", type: "integer", nullable: false, oldClrType: typeof(int), oldType: "integer", oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_AwayTeamId",
                table: "Matches",
                column: "AwayTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_HomeTeamId",
                table: "Matches",
                column: "HomeTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_Active",
                table: "Teams",
                column: "Active");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_Name",
                table: "Teams",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Teams_AwayTeamId",
                table: "Matches",
                column: "AwayTeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Teams_HomeTeamId",
                table: "Matches",
                column: "HomeTeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Teams_AwayTeamId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Teams_HomeTeamId",
                table: "Matches");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Matches_AwayTeamId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_HomeTeamId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AwayTeamId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeTeamId",
                table: "Matches");
        }
    }
}
