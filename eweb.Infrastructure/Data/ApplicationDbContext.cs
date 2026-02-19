using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using eweb.Infrastructure.Identity;
using eweb.Domain.Entities;

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
    }
}
