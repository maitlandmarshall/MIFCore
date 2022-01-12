using Microsoft.EntityFrameworkCore.Migrations;

namespace MIFCore.Hangfire.JobActions.Database.Migrations
{
    public partial class JobActions_Database : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Database",
                schema: "job",
                table: "JobActions",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Database",
                schema: "job",
                table: "JobActions");
        }
    }
}
