using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Poll> Polls { get; set; }
    public DbSet<PollOption> PollOptions { get; set; }
    public DbSet<PollSubmission> PollSubmissions { get; set; }
    public DbSet<PollSubmissionSelection> PollSubmissionSelections { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure composite primary key for PollSubmissionSelection
        modelBuilder.Entity<PollSubmissionSelection>()
            .HasKey(x => new { x.PollSubmissionId, x.PollOptionId });

        // Configure unique constraint for user submissions (one submission per user per poll)
        modelBuilder.Entity<PollSubmission>()
            .HasIndex(x => new { x.UserId, x.PollId })
            .IsUnique();

        // Configure relationships
        modelBuilder.Entity<PollOption>()
            .HasOne(po => po.Poll)
            .WithMany(p => p.Options)
            .HasForeignKey(po => po.PollId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PollSubmission>()
            .HasOne(ps => ps.Poll)
            .WithMany(p => p.Submissions)
            .HasForeignKey(ps => ps.PollId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PollSubmissionSelection>()
            .HasOne(pss => pss.PollSubmission)
            .WithMany(ps => ps.PollSubmissionSelections)
            .HasForeignKey(pss => pss.PollSubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PollSubmissionSelection>()
            .HasOne(pss => pss.PollOption)
            .WithMany()
            .HasForeignKey(pss => pss.PollOptionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure default values
        modelBuilder.Entity<Poll>()
            .Property(p => p.CreatedAt)
            .HasDefaultValueSql("NOW()");

        modelBuilder.Entity<PollOption>()
            .Property(po => po.CreatedAt)
            .HasDefaultValueSql("NOW()");

        modelBuilder.Entity<PollSubmission>()
            .Property(ps => ps.CreatedAt)
            .HasDefaultValueSql("NOW()");
    }
}