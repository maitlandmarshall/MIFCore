using Microsoft.EntityFrameworkCore.Migrations;

namespace MIFCore.Hangfire.JobActions.Database.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "job");

            migrationBuilder.CreateTable(
                name: "JobActions",
                schema: "job",
                columns: table => new
                {
                    JobName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Order = table.Column<long>(type: "bigint", nullable: false),
                    Timing = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobActions", x => new { x.JobName, x.Action, x.Order });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobActions",
                schema: "job");
        }
    }
}
