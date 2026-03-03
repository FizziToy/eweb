using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eweb.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseAttemptSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExerciseAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExerciseId = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaskAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExerciseAttemptId = table.Column<int>(type: "int", nullable: false),
                    ExerciseTaskId = table.Column<int>(type: "int", nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    AttemptedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskAttempts_ExerciseAttempts_ExerciseAttemptId",
                        column: x => x.ExerciseAttemptId,
                        principalTable: "ExerciseAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskAttempts_ExerciseAttemptId",
                table: "TaskAttempts",
                column: "ExerciseAttemptId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskAttempts");

            migrationBuilder.DropTable(
                name: "ExerciseAttempts");
        }
    }
}
