using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PlayPredict.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddExperiences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UseExperienceDefaults",
                table: "EditionScoringConfigurations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Experiences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PrimaryColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SecondaryColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultExactScorePoints = table.Column<int>(type: "integer", nullable: false),
                    DefaultCorrectOutcomePoints = table.Column<int>(type: "integer", nullable: false),
                    DefaultIncorrectPoints = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Experiences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Experiences_Name",
                table: "Experiences",
                column: "Name");

            // Experiencia de demostración para mantener compatibilidad: toda Competencia
            // existente (de cualquier entorno) queda asociada a esta Experience, ya que
            // ExperienceId pasa a ser obligatorio a partir de esta migración. No se pierde
            // ningún dato existente.
            migrationBuilder.Sql(@"
                INSERT INTO ""Experiences""
                    (""Name"", ""Description"", ""Status"", ""PrimaryColor"", ""SecondaryColor"", ""LogoUrl"", ""IsPublic"",
                     ""DefaultExactScorePoints"", ""DefaultCorrectOutcomePoints"", ""DefaultIncorrectPoints"",
                     ""CreatedAtUtc"", ""UpdatedAtUtc"")
                VALUES
                    ('PlayPredict Demo',
                     'Experiencia de demostración generada automáticamente para mantener la compatibilidad de las Competencias existentes al incorporar el modelo de Experiencias.',
                     1, NULL, NULL, NULL, TRUE, 6, 3, 0, now(), now());
            ");

            migrationBuilder.AddColumn<int>(
                name: "ExperienceId",
                table: "Competitions",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE ""Competitions""
                SET ""ExperienceId"" = (SELECT ""Id"" FROM ""Experiences"" WHERE ""Name"" = 'PlayPredict Demo' LIMIT 1)
                WHERE ""ExperienceId"" IS NULL;
            ");

            migrationBuilder.AlterColumn<int>(
                name: "ExperienceId",
                table: "Competitions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Competitions_ExperienceId",
                table: "Competitions",
                column: "ExperienceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Competitions_Experiences_ExperienceId",
                table: "Competitions",
                column: "ExperienceId",
                principalTable: "Experiences",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Competitions_Experiences_ExperienceId",
                table: "Competitions");

            migrationBuilder.DropIndex(
                name: "IX_Competitions_ExperienceId",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "ExperienceId",
                table: "Competitions");

            migrationBuilder.DropTable(
                name: "Experiences");

            migrationBuilder.DropColumn(
                name: "UseExperienceDefaults",
                table: "EditionScoringConfigurations");
        }
    }
}
