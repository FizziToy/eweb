using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eweb.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInteractiveExercises : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InteractiveExercises",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LessonId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InteractiveExercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InteractiveExercises_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExerciseId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StarsReward = table.Column<int>(type: "int", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseTasks_InteractiveExercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "InteractiveExercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseTasks_ExerciseId",
                table: "ExerciseTasks",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_InteractiveExercises_LessonId",
                table: "InteractiveExercises",
                column: "LessonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExerciseTasks");

            migrationBuilder.DropTable(
                name: "InteractiveExercises");
        }
    }
}
