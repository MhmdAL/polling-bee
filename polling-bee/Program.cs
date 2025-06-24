using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("MyInMemoryDb"));

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Only seed if empty
    // if (!context.Polls.Any())
    // {

    context.PollOptions.AddRange(
        new List<PollOption>() {
            new PollOption {
                Id = 1,
                PollId = 1,
                Name = "option1",
                Votes = 0
            },
            new PollOption {
                Id = 2,
                PollId = 1,
                Name = "option2",
                Votes = 0
            },

            new PollOption {
                Id = 3,
                PollId = 2,
                Name = "oooption1",
                Votes = 0
            },
            new PollOption {
                Id = 4,
                PollId = 2,
                Name = "oooption2",
                Votes = 0
            },
            new PollOption {
                Id = 5,
                PollId = 2,
                Name = "oooption1",
                Votes = 0
            },
            new PollOption {
                Id = 6,
                PollId = 2,
                Name = "oooption2",
                Votes = 0
            }
        });

    context.Polls.AddRange(
        new Poll
        {
            Id = 1,
            Question = "What is your favorite color?",
            MaxResponseOptions = 1
        },
        new Poll
        {
            Id = 2,
            Question = "What is your favorite country?",
            MaxResponseOptions = 2
        }
    );
    context.SaveChanges();

    Console.WriteLine("seeded stuff");
    // }
}

app.MapPost("/createPoll", ([FromBody] Poll request, AppDbContext dbContext) =>
{
    var poll = new Poll
    {
        Question = request.Question,
        MaxResponseOptions = request.MaxResponseOptions,
        Options = request.Options
    };

    dbContext.Polls.Add(poll);

    dbContext.SaveChanges();

    return poll.Id;
});

app.MapGet("/getPoll/{pollId}/{userId}", (string userId, int pollId, AppDbContext dbContext) =>
{
    var poll = dbContext.Polls
        .Include(x => x.Options)
        .Include(x => x.Submissions)
            .ThenInclude(x => x.PollSubmissionSelections)
        .FirstOrDefault(x => x.Id == pollId);

    return new
    {
        AlreadySubmitted = poll != null ? poll.Submissions?.FirstOrDefault(x => x.UserId == userId) != null : false,
        SelectedOptions = poll.Submissions?.FirstOrDefault(x => x.UserId == userId)?.PollSubmissionSelections?.Select(x => x.PollOptionId) ?? new List<int>(),
        Poll = poll ?? null
    };
});

app.MapGet("/getPolls", (AppDbContext dbContext) =>
{
    var polls = dbContext.Polls
        .Include(x => x.Options)
        .Include(x => x.Submissions)
        .ToList();

    return polls;
});

app.MapPost("/submitPoll/{userId}", (string userId, [FromBody] SubmitPollRequest request, AppDbContext dbContext) =>
{
    var poll = dbContext.Polls
        .Include(x => x.Options)
        .FirstOrDefault(x => x.Id == request.PollId);

    var pollSubmission = new PollSubmission
    {
        PollId = poll.Id,
        UserId = userId
    };

    var pollSubmissionSelections = new List<PollSubmissionSelection>();

    foreach (var item in request.PollOptionIds)
    {
        poll.Options.FirstOrDefault(x => x.Id == item).Votes++;

        pollSubmissionSelections.Add(new PollSubmissionSelection
        {
            PollOptionId = item,
        });
    }

    pollSubmission.PollSubmissionSelections = pollSubmissionSelections;

    dbContext.PollSubmissions.Add(pollSubmission);

    dbContext.SaveChanges();

    return true;
});

app.Run();

public class SubmitPollRequest
{
    public int PollId { get; set; }
    public List<int> PollOptionIds { get; set; }
}