
public class Poll
{
    public int Id { get; set; }
    public string Question { get; set; }
    public int MaxResponseOptions { get; set; }
    public List<PollOption> Options { get; set; }
    public List<PollSubmission> Submissions { get; set; }
}

public class PollOption
{
    public int Id { get; set; }
    public int PollId { get; set; }

    public string Name { get; set; }
    public int Votes { get; set; }
}

public class PollSubmission
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public int PollId { get; set; }
    public List<PollSubmissionSelection> PollSubmissionSelections { get; set; }
}

public class PollSubmissionSelection
{
    public int PollSubmissionId { get; set; }
    public int PollOptionId { get; set; }
}