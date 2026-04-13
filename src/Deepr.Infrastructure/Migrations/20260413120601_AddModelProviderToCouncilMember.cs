using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Deepr.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddModelProviderToCouncilMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModelId",
                table: "CouncilMember",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelProvider",
                table: "CouncilMember",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModelId",
                table: "CouncilMember");

            migrationBuilder.DropColumn(
                name: "ModelProvider",
                table: "CouncilMember");
        }
    }
}
