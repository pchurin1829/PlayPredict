using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlayPredict.Api.Migrations
{
    public partial class AddLeagueType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LeagueType",
                table: "Leagues",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            // Actualizar Liga demo a Official
            migrationBuilder.Sql("UPDATE \"Leagues\" SET \"LeagueType\" = 0 WHERE \"Name\" LIKE '%Liga General%' OR \"Name\" LIKE '%(demo)%'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LeagueType",
                table: "Leagues");
        }
    }
}
