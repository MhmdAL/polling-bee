using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors();

// Configure JSON options to handle circular references
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

// Configure Entity Framework with PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// If no connection string in config, build from environment variables
if (string.IsNullOrEmpty(connectionString))
{
    var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
    var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
    var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
    var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "password";
    
    connectionString = $"Host={dbHost};Port={dbPort};Username={dbUser};Password={dbPassword}";
}

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Database connection string could not be determined.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.CommandTimeout(600); // 10 minutes
        npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
    }));

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
                    CreatedBy = "system",
                    Options = new List<PollOption>
                    {
                        new PollOption { Name = "Red" },
                        new PollOption { Name = "Blue" }
                    }
                },
                new Poll
                {
                    Question = "What is your favorite country?",
                    MaxResponseOptions = 2,
                    CreatedBy = "system",
                    Options = new List<PollOption>
                    {
                        new PollOption { Name = "USA" },
                        new PollOption { Name = "Canada" },
                        new PollOption { Name = "UK" },
                        new PollOption { Name = "Australia" }
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
        CreatedBy = request.CreatedBy,
        Options = request.Options?.Select(o => new PollOption
        {
            Name = o.Name
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

app.MapGet("/getPollResults/{pollId}", async (int pollId, AppDbContext dbContext) =>
{
    var poll = await dbContext.Polls
        .Include(p => p.Options)
        .Include(p => p.Submissions)
            .ThenInclude(s => s.PollSubmissionSelections)
        .FirstOrDefaultAsync(p => p.Id == pollId);
    
    if (poll == null)
    {
        return new { Poll = (Poll?)null, SubmissionCount = 0 };
    }

    return new
    {
        Poll = poll,
        SubmissionCount = poll.SubmissionCount
    };
});


app.MapGet("/getPolls/{createdBy}", async (string createdBy, AppDbContext dbContext) =>
{
    var polls = await dbContext.Polls
        .Include(p => p.Options)
        .Include(p => p.Submissions)
        .Where(x => x.CreatedBy == createdBy)
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
        await dbContext.SaveChangesAsync();

        return Results.Ok(true);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error submitting poll: {ex.Message}");
        return Results.Problem("Failed to submit poll response");
    }
});

app.Run();

public class SubmitPollRequest
{
    public int PollId { get; set; }
    public List<int> PollOptionIds { get; set; }
}