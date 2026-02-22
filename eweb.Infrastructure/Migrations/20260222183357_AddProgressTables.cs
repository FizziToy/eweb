using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eweb.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProgressTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserExerciseTaskProgresses",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExerciseTaskId = table.Column<int>(type: "int", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserExerciseTaskProgresses", x => new { x.UserId, x.ExerciseTaskId });
                });

            migrationBuilder.CreateTable(
                name: "UserLessonProgresses",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LessonId = table.Column<int>(type: "int", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLessonProgresses", x => new { x.UserId, x.LessonId });
                });

            migrationBuilder.CreateTable(
                name: "UserQuestionProgresses",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserQuestionProgresses", x => new { x.UserId, x.QuestionId });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserExerciseTaskProgresses");

            migrationBuilder.DropTable(
                name: "UserLessonProgresses");

            migrationBuilder.DropTable(
                name: "UserQuestionProgresses");
        }
    }
}
