using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WcagAnalyzer.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeepScanAndPageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PageUrl",
                table: "AnalysisResults",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DeepScan",
                table: "AnalysisRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PageUrl",
                table: "AnalysisResults");

            migrationBuilder.DropColumn(
                name: "DeepScan",
                table: "AnalysisRequests");
        }
    }
}
