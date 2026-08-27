using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlayPredict.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLeagueSourceLeague : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourceLeagueId",
                table: "Leagues",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leagues_SourceLeagueId",
                table: "Leagues",
                column: "SourceLeagueId");

            migrationBuilder.AddForeignKey(
                name: "FK_Leagues_Leagues_SourceLeagueId",
                table: "Leagues",
                column: "SourceLeagueId",
                principalTable: "Leagues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leagues_Leagues_SourceLeagueId",
                table: "Leagues");

            migrationBuilder.DropIndex(
                name: "IX_Leagues_SourceLeagueId",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "SourceLeagueId",
                table: "Leagues");
        }
    }
}
