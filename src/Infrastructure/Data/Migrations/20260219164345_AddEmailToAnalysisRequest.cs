using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WcagAnalyzer.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailToAnalysisRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "AnalysisRequests",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "AnalysisRequests");
        }
    }
}
