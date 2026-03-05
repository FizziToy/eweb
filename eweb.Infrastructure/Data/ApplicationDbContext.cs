using eweb.Domain.Entities;
using eweb.Domain.Entities.Attempts;
using eweb.Domain.Entities.Exercises;
using eweb.Domain.Entities.Progress;
using eweb.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

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
    public DbSet<LessonTestAttempt> LessonTestAttempts { get; set; }
    public DbSet<ExerciseAttempt> ExerciseAttempts { get; set; }
    public DbSet<TaskAttempt> TaskAttempts { get; set; }
    public DbSet<UserExerciseProgress> UserExerciseProgresses { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Lesson>().HasKey(l => l.Id);
        builder.Entity<TheoryQuestion>().HasKey(q => q.Id);
        builder.Entity<AnswerOption>().HasKey(a => a.Id);

        builder.Entity<Lesson>()
            .HasMany(l => l.Questions)
            .WithOne(q => q.Lesson)
            .HasForeignKey(q => q.LessonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Lesson>()
            .Navigation(nameof(Lesson.Questions))
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Entity<TheoryQuestion>()
            .HasMany(q => q.AnswerOptions)
            .WithOne()
            .HasForeignKey(a => a.TheoryQuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TheoryQuestion>()
            .Navigation(nameof(TheoryQuestion.AnswerOptions))
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Entity<InteractiveExercise>()
            .HasOne<Lesson>()
            .WithMany()
            .HasForeignKey(e => e.LessonId)
            .OnDelete(DeleteBehavior.Cascade);

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

        builder.Entity<UserExerciseProgress>()
            .HasKey(x => new { x.UserId, x.ExerciseId });

        builder.Entity<ExerciseAttempt>()
            .HasMany(e => e.TaskAttempts)
            .WithOne()
            .HasForeignKey(t => t.ExerciseAttemptId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
