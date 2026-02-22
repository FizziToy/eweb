using eweb.Domain.Entities;
using eweb.Domain.Entities.Progress;
using eweb.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace eweb.Infrastructure.Data;

public class ApplicationDbContext: IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<TheoryQuestion> TheoryQuestions { get; set; } = null!;
    public DbSet<AnswerOption> AnswerOptions { get; set; } = null!;
    public DbSet<UserLessonProgress> UserLessonProgresses { get; set; }
    public DbSet<UserQuestionProgress> UserQuestionProgresses { get; set; }
    public DbSet<UserExerciseTaskProgress> UserExerciseTaskProgresses { get; set; }

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

        builder.Entity<UserLessonProgress>()
            .HasKey(x => new { x.UserId, x.LessonId });

        builder.Entity<UserQuestionProgress>()
            .HasKey(x => new { x.UserId, x.QuestionId });

        builder.Entity<UserExerciseTaskProgress>()
            .HasKey(x => new { x.UserId, x.ExerciseTaskId });
    }
}
