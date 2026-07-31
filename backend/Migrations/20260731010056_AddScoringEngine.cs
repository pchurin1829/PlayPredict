using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PlayPredict.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddScoringEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EditionScoringConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EditionId = table.Column<int>(type: "integer", nullable: false),
                    ExactScorePoints = table.Column<int>(type: "integer", nullable: false),
                    CorrectOutcomePoints = table.Column<int>(type: "integer", nullable: false),
                    IncorrectPoints = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EditionScoringConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EditionScoringConfigurations_Editions_EditionId",
                        column: x => x.EditionId,
                        principalTable: "Editions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PredictionEvaluations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PredictionId = table.Column<int>(type: "integer", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    EvaluationType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OfficialHomeScore = table.Column<int>(type: "integer", nullable: false),
                    OfficialAwayScore = table.Column<int>(type: "integer", nullable: false),
                    AppliedRuleValue = table.Column<int>(type: "integer", nullable: false),
                    EvaluatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PredictionEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PredictionEvaluations_Predictions_PredictionId",
                        column: x => x.PredictionId,
                        principalTable: "Predictions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EditionScoringConfigurations_EditionId",
                table: "EditionScoringConfigurations",
                column: "EditionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PredictionEvaluations_PredictionId",
                table: "PredictionEvaluations",
                column: "PredictionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EditionScoringConfigurations");

            migrationBuilder.DropTable(
                name: "PredictionEvaluations");
        }
    }
}
