using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskUp.Migrations
{
    /// <inheritdoc />
    public partial class FixBoardTaskPriorityToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Priority",
                table: "BoardTasks",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "Medium",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Medium");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Priority",
                table: "BoardTasks",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Medium",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldDefaultValue: "Medium");
        }
    }
}
