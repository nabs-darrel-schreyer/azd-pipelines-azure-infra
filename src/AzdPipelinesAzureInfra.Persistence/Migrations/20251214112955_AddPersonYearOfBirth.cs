using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AzdPipelinesAzureInfra.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonYearOfBirth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "YearOfBirth",
                schema: "test",
                table: "People",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "YearOfBirth",
                schema: "test",
                table: "People");
        }
    }
}
