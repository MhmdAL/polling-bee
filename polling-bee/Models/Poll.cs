
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

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
    
    [Column("created_by")]
    public string? CreatedBy { get; set; }

    public int SubmissionCount  => Submissions?.Count ?? 0;
    
    // Navigation properties
    public virtual ICollection<PollOption> Options { get; set; } = new List<PollOption>();
    public virtual ICollection<PollSubmission> Submissions { get; set; } = new List<PollSubmission>();
    
    // Helper method to get vote counts efficiently
    public Dictionary<int, int> GetVoteCounts()
    {
        return Submissions
            .SelectMany(s => s.PollSubmissionSelections)
            .GroupBy(sel => sel.PollOptionId)
            .ToDictionary(g => g.Key, g => g.Count());
    }
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
    
    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }
    
    // Computed property - votes calculated from submissions
    [NotMapped]
    public int Votes 
    { 
        get
        {
            if (Poll?.Submissions != null)
            {
                return Poll.Submissions
                    .SelectMany(s => s.PollSubmissionSelections)
                    .Count(sel => sel.PollOptionId == Id);
            }
            return 0;
        }
    }
    
    // Navigation properties
    [ForeignKey("PollId")]
    [JsonIgnore] // Prevent circular reference
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
    [JsonIgnore] // Prevent circular reference
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
    [JsonIgnore] // Prevent circular reference
    public virtual PollSubmission PollSubmission { get; set; } = null!;
    
    [ForeignKey("PollOptionId")]
    [JsonIgnore] // Prevent circular reference
    public virtual PollOption PollOption { get; set; } = null!;
}