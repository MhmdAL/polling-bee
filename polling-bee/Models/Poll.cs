
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("polls")]
public class Poll
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Required]
    [Column("question")]
    public string Question { get; set; } = string.Empty;
    
    [Column("max_response_options")]
    public int MaxResponseOptions { get; set; }
    
    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<PollOption> Options { get; set; } = new List<PollOption>();
    public virtual ICollection<PollSubmission> Submissions { get; set; } = new List<PollSubmission>();
}

[Table("poll_options")]
public class PollOption
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Required]
    [Column("poll_id")]
    public int PollId { get; set; }
    
    [Required]
    [Column("name")]
    public string Name { get; set; } = string.Empty;
    
    [Column("votes")]
    public int Votes { get; set; }
    
    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }
    
    // Navigation properties
    [ForeignKey("PollId")]
    public virtual Poll Poll { get; set; } = null!;
}

[Table("poll_submissions")]
public class PollSubmission
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Required]
    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [Column("poll_id")]
    public int PollId { get; set; }
    
    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }
    
    // Navigation properties
    [ForeignKey("PollId")]
    public virtual Poll Poll { get; set; } = null!;
    public virtual ICollection<PollSubmissionSelection> PollSubmissionSelections { get; set; } = new List<PollSubmissionSelection>();
}

[Table("poll_submission_selections")]
public class PollSubmissionSelection
{
    [Required]
    [Column("poll_submission_id")]
    public int PollSubmissionId { get; set; }
    
    [Required]
    [Column("poll_option_id")]
    public int PollOptionId { get; set; }
    
    // Navigation properties
    [ForeignKey("PollSubmissionId")]
    public virtual PollSubmission PollSubmission { get; set; } = null!;
    
    [ForeignKey("PollOptionId")]
    public virtual PollOption PollOption { get; set; } = null!;
}