using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlayPredict.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueActiveWelcomeCampaignPerCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WelcomeCampaigns_CompanyId_ActiveOnly",
                table: "WelcomeCampaigns",
                column: "CompanyId",
                unique: true,
                filter: "\"IsActive\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WelcomeCampaigns_CompanyId_ActiveOnly",
                table: "WelcomeCampaigns");
        }
    }
}
