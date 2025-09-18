using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors();

// Configure Entity Framework with PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

// Initialize database and seed data if needed
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    try
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();
        
        // Check if polls already exist
        if (!await context.Polls.AnyAsync())
        {
            // Seed initial polls
            var polls = new List<Poll>
            {
                new Poll
                {
                    Question = "What is your favorite color?",
                    MaxResponseOptions = 1,
                    Options = new List<PollOption>
                    {
                        new PollOption { Name = "Red", Votes = 0 },
                        new PollOption { Name = "Blue", Votes = 0 }
                    }
                },
                new Poll
                {
                    Question = "What is your favorite country?",
                    MaxResponseOptions = 2,
                    Options = new List<PollOption>
                    {
                        new PollOption { Name = "USA", Votes = 0 },
                        new PollOption { Name = "Canada", Votes = 0 },
                        new PollOption { Name = "UK", Votes = 0 },
                        new PollOption { Name = "Australia", Votes = 0 }
                    }
                }
            };

            context.Polls.AddRange(polls);
            await context.SaveChangesAsync();

            Console.WriteLine("Seeded initial data to PostgreSQL");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database initialization failed: {ex.Message}");
        // Continue anyway - tables might already exist
    }
}





app.MapPost("/createPoll", async ([FromBody] Poll request, AppDbContext dbContext) =>
{
    var poll = new Poll
    {
        Question = request.Question,
        MaxResponseOptions = request.MaxResponseOptions,
        Options = request.Options?.Select(o => new PollOption
        {
            Name = o.Name,
            Votes = 0
        }).ToList() ?? new List<PollOption>()
    };

    dbContext.Polls.Add(poll);
    await dbContext.SaveChangesAsync();

    return poll.Id;
});

app.MapGet("/getPoll/{pollId}/{userId}", async (string userId, int pollId, AppDbContext dbContext) =>
{
    var poll = await dbContext.Polls
        .Include(p => p.Options)
        .Include(p => p.Submissions)
            .ThenInclude(s => s.PollSubmissionSelections)
        .FirstOrDefaultAsync(p => p.Id == pollId);
    
    if (poll == null)
    {
        return new
        {
            AlreadySubmitted = false,
            SelectedOptions = new List<int>(),
            Poll = (Poll?)null
        };
    }

    // Check if user already submitted
    var userSubmission = poll.Submissions?.FirstOrDefault(s => s.UserId == userId);
    var alreadySubmitted = userSubmission != null;

    var selectedOptions = userSubmission?.PollSubmissionSelections?.Select(s => s.PollOptionId).ToList() ?? new List<int>();

    return new
    {
        AlreadySubmitted = alreadySubmitted,
        SelectedOptions = selectedOptions,
        Poll = poll
    };
});

app.MapGet("/getPolls", async (AppDbContext dbContext) =>
{
    var polls = await dbContext.Polls
        .Include(p => p.Options)
        .Include(p => p.Submissions)
        .ToListAsync();

    return polls;
});

app.MapPost("/submitPoll/{userId}", async (string userId, [FromBody] SubmitPollRequest request, AppDbContext dbContext) =>
{
    // Check if user already submitted
    var existingSubmission = await dbContext.PollSubmissions
        .FirstOrDefaultAsync(s => s.UserId == userId && s.PollId == request.PollId);
    
    if (existingSubmission != null)
    {
        return Results.BadRequest("User has already submitted for this poll");
    }

    // Use transaction for data consistency
    using var transaction = await dbContext.Database.BeginTransactionAsync();
    
    try
    {
        // Create poll submission
        var pollSubmission = new PollSubmission
        {
            PollId = request.PollId,
            UserId = userId,
            PollSubmissionSelections = request.PollOptionIds.Select(optionId => new PollSubmissionSelection
            {
                PollOptionId = optionId
            }).ToList()
        };

        dbContext.PollSubmissions.Add(pollSubmission);

        // Update vote counts for selected options
        var optionsToUpdate = await dbContext.PollOptions
            .Where(o => request.PollOptionIds.Contains(o.Id))
            .ToListAsync();

        foreach (var option in optionsToUpdate)
        {
            option.Votes++;
        }

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return Results.Ok(true);
    }
    catch (Exception)
    {
        await transaction.RollbackAsync();
        return Results.Problem("Failed to submit poll response");
    }
});

app.Run();

public class SubmitPollRequest
{
    public int PollId { get; set; }
    public List<int> PollOptionIds { get; set; }
}