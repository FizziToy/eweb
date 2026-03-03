using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eweb.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserExerciseProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FinishedAt",
                table: "ExerciseAttempts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFinished",
                table: "ExerciseAttempts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "UserExerciseProgresses",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExerciseId = table.Column<int>(type: "int", nullable: false),
                    MaxCorrectTasks = table.Column<int>(type: "int", nullable: false),
                    IsFullyCompleted = table.Column<bool>(type: "bit", nullable: false),
                    AdditionalAttempts = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserExerciseProgresses", x => new { x.UserId, x.ExerciseId });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserExerciseProgresses");

            migrationBuilder.DropColumn(
                name: "FinishedAt",
                table: "ExerciseAttempts");

            migrationBuilder.DropColumn(
                name: "IsFinished",
                table: "ExerciseAttempts");
        }
    }
}
