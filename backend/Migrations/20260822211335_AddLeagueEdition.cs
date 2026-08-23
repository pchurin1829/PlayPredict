using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlayPredict.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLeagueEdition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EditionId",
                table: "Leagues",
                type: "integer",
                nullable: true);

            // Conserva las Ligas existentes: para rangos toma la Edición de la Fecha
            // inicial; para alcance completo toma la Edición más reciente de su Competencia.
            migrationBuilder.Sql("""
                UPDATE "Leagues" AS l
                SET "EditionId" = COALESCE(
                    (SELECT r."EditionId" FROM "Rounds" AS r WHERE r."Id" = l."RoundFromId"),
                    (SELECT e."Id" FROM "Editions" AS e
                     WHERE e."CompetitionId" = l."CompetitionId"
                     ORDER BY e."StartDateUtc" DESC, e."Id" DESC
                     LIMIT 1)
                );
                """);

            migrationBuilder.AlterColumn<int>(
                name: "EditionId",
                table: "Leagues",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leagues_EditionId",
                table: "Leagues",
                column: "EditionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Leagues_Editions_EditionId",
                table: "Leagues",
                column: "EditionId",
                principalTable: "Editions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leagues_Editions_EditionId",
                table: "Leagues");

            migrationBuilder.DropIndex(
                name: "IX_Leagues_EditionId",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "EditionId",
                table: "Leagues");
        }
    }
}
