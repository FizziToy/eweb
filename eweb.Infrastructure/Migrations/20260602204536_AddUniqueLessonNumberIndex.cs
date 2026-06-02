using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eweb.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueLessonNumberIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserQuestionProgresses_QuestionId",
                table: "UserQuestionProgresses",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLessonProgresses_LessonId",
                table: "UserLessonProgresses",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonTestAttempts_LessonId",
                table: "LessonTestAttempts",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_Number",
                table: "Lessons",
                column: "Number",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LessonTestAttempts_Lessons_LessonId",
                table: "LessonTestAttempts",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserLessonProgresses_Lessons_LessonId",
                table: "UserLessonProgresses",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserQuestionProgresses_TheoryQuestions_QuestionId",
                table: "UserQuestionProgresses",
                column: "QuestionId",
                principalTable: "TheoryQuestions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LessonTestAttempts_Lessons_LessonId",
                table: "LessonTestAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_UserLessonProgresses_Lessons_LessonId",
                table: "UserLessonProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_UserQuestionProgresses_TheoryQuestions_QuestionId",
                table: "UserQuestionProgresses");

            migrationBuilder.DropIndex(
                name: "IX_UserQuestionProgresses_QuestionId",
                table: "UserQuestionProgresses");

            migrationBuilder.DropIndex(
                name: "IX_UserLessonProgresses_LessonId",
                table: "UserLessonProgresses");

            migrationBuilder.DropIndex(
                name: "IX_LessonTestAttempts_LessonId",
                table: "LessonTestAttempts");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_Number",
                table: "Lessons");
        }
    }
}
