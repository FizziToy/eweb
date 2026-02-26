using eweb.Domain.Entities;
using eweb.Domain.Entities.Progress;
using eweb.Domain.Entities.Exercises;
using eweb.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace eweb.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<TheoryQuestion> TheoryQuestions { get; set; } = null!;
    public DbSet<AnswerOption> AnswerOptions { get; set; } = null!;
    public DbSet<UserLessonProgress> UserLessonProgresses { get; set; }
    public DbSet<UserQuestionProgress> UserQuestionProgresses { get; set; }
    public DbSet<UserExerciseTaskProgress> UserExerciseTaskProgresses { get; set; }
    public DbSet<InteractiveExercise> InteractiveExercises { get; set; }
    public DbSet<ExerciseTask> ExerciseTasks { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Lesson - TheoryQuestion (1 : many)
        builder.Entity<Lesson>()
            .HasMany(l => l.Questions)
            .WithOne()
            .HasForeignKey("LessonId")
            .OnDelete(DeleteBehavior.Cascade);

        // TheoryQuestion - AnswerOption (1 : many)
        builder.Entity<TheoryQuestion>()
            .HasMany(q => q.AnswerOptions)
            .WithOne()
            .HasForeignKey("TheoryQuestionId")
            .OnDelete(DeleteBehavior.Cascade);

        // Lesson → InteractiveExercise (1 : many)
        builder.Entity<InteractiveExercise>()
            .HasOne<Lesson>()
            .WithMany()
            .HasForeignKey(e => e.LessonId)
            .OnDelete(DeleteBehavior.Cascade);

        // InteractiveExercise → ExerciseTask (1 : many)
        builder.Entity<ExerciseTask>()
            .HasOne<InteractiveExercise>()
            .WithMany(e => e.Tasks)
            .HasForeignKey(t => t.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserLessonProgress>()
            .HasKey(x => new { x.UserId, x.LessonId });

        builder.Entity<UserQuestionProgress>()
            .HasKey(x => new { x.UserId, x.QuestionId });

        builder.Entity<UserExerciseTaskProgress>()
            .HasKey(x => new { x.UserId, x.ExerciseTaskId });
    }
}
