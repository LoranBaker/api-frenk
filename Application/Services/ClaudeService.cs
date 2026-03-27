using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Frank.Application.DTOs.Responses;
using Frank.Application.Interfaces.Services;
using Frank.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Frank.Application.Services;

/// <summary>
/// Calls the Anthropic Claude API.
/// MVP calls: morning forecast + weekly mirror observations + reflection question + mini mirror.
/// 
/// Rules:
///   - Never throws — always returns fallback on failure
///   - Never logs context content — logs user_id + event type only
///   - Always respects max token limits
///   - Sacred fields (pre_urge_description + core_motivation) in every prompt
/// </summary>
public class ClaudeService : IClaudeService
{
    private readonly HttpClient _http;
    private readonly IAppSettingsService _settings;
    private readonly ILogger<ClaudeService> _logger;
    private readonly string _apiKey;

    private const string Model = "claude-sonnet-4-5";
    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const double Temperature = 0.7;

    public ClaudeService(
        HttpClient http,
        IAppSettingsService settings,
        IConfiguration config,
        ILogger<ClaudeService> logger)
    {
        _http = http;
        _settings = settings;
        _logger = logger;
        _apiKey = config["Claude:ApiKey"]
            ?? throw new InvalidOperationException("Claude:ApiKey not configured.");
    }

    // ── Morning Forecast ──────────────────────────────────

    public async Task<string> GetMorningForecastAsync(
        MorningForecastContext context,
        CancellationToken ct = default)
    {
        try
        {
            var systemPrompt =
                "You are Frank — a direct, honest addiction companion. " +
                "You are not a therapist. You do not give generic encouragement. " +
                "You pay attention to patterns and tell the truth. " +
                "Be brief. Never use the word \"journey\". Never say \"you've got this\". " +
                "Speak like a person who knows them, not an app.";

            var userPrompt = BuildMorningPrompt(context);

            var response = await CallClaudeAsync(
                systemPrompt: systemPrompt,
                userPrompt: userPrompt,
                maxTokens: 80,
                ct: ct);

            _logger.LogInformation(
                "Morning forecast generated for user {UserId}",
                context.Profile.UserId);

            return string.IsNullOrWhiteSpace(response)
                ? _settings.GetMorningForecastFallback()
                : response.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Claude morning forecast failed for user {UserId}",
                context.Profile.UserId);

            return _settings.GetMorningForecastFallback();
        }
    }

    // ── Weekly Mirror ─────────────────────────────────────

    public async Task<List<ObservationWithQuotes>> GetWeeklyObservationsAsync(
        WeeklyMirrorContext context,
        CancellationToken ct = default)
    {
        try
        {
            var systemPrompt =
                "You are Frank — a direct, honest addiction companion. " +
                "You are reviewing one user's week. You have real data. Use it. " +
                "Do not be vague. Do not be motivational. " +
                "Be honest like a good friend who has been watching. " +
                "If the week was bad, say so clearly but without shame. " +
                "If there was a pattern, name it precisely.";

            var userPrompt = BuildWeeklyPrompt(context);

            var raw = await CallClaudeAsync(
                systemPrompt: systemPrompt,
                userPrompt: userPrompt,
                maxTokens: 400,
                ct: ct);

            return ParseWeeklyObservations(raw);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Claude weekly mirror failed for user {UserId}",
                context.Profile.UserId);

            return new List<ObservationWithQuotes>();
        }
    }

    // ── Reflection Question ───────────────────────────────

    public async Task<string?> GetReflectionQuestionAsync(
        ReflectionContext context,
        CancellationToken ct = default)
    {
        try
        {
            var systemPrompt =
                "You are Frank. You ask one question at the end of the weekly mirror. " +
                "The question should make the user think about their own pattern — " +
                "not feel guilty, not feel praised. " +
                "It should be a genuine question you are curious about based on their specific week.";

            var userPrompt =
                $"Week observations:\n" +
                $"1. {context.Observation1}\n" +
                $"2. {context.Observation2}\n" +
                $"3. {context.Observation3}\n\n" +
                $"Recent questions shown (do not repeat structure):\n" +
                string.Join("\n", context.RecentQuestionIds) +
                "\n\nWrite ONE reflection question. Max 20 words. End with question mark.";

            var response = await CallClaudeAsync(
                systemPrompt: systemPrompt,
                userPrompt: userPrompt,
                maxTokens: 50,
                ct: ct);

            return string.IsNullOrWhiteSpace(response) ? null : response.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Claude reflection question failed.");
            return null;
        }
    }

    // ── Mini Mirror ───────────────────────────────────────

    public async Task<string> GetMiniMirrorObservationAsync(
        List<string> quotes,
        UserProfile profile,
        CancellationToken ct = default)
    {
        try
        {
            var systemPrompt =
                "You are Frank. The user is new. " +
                "You have their first few urge session notes. " +
                "Make one honest observation in one sentence. " +
                "Reference what they actually wrote. No fluff.";

            var userPrompt =
                $"User: {profile.FirstName}\n" +
                $"Addiction: {string.Join(", ", profile.AddictionTypes)}\n\n" +
                $"What they wrote before pressing the button:\n" +
                string.Join("\n", quotes.Select((q, i) => $"- {q}")) +
                "\n\nOne sentence observation. Be specific. Reference their words.";

            var response = await CallClaudeAsync(
                systemPrompt: systemPrompt,
                userPrompt: userPrompt,
                maxTokens: 60,
                ct: ct);

            return string.IsNullOrWhiteSpace(response)
                ? "Something is already showing up in these early sessions."
                : response.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Claude mini mirror failed for user {UserId}", profile.UserId);

            return "Something is already showing up in these early sessions.";
        }
    }

    // ── Core API call ─────────────────────────────────────

    private async Task<string> CallClaudeAsync(
    string systemPrompt,
    string userPrompt,
    int maxTokens,
    CancellationToken ct)
    {
        var payload = new
        {
            model = Model,
            max_tokens = maxTokens,
            temperature = Temperature,
            system = systemPrompt,
            messages = new[]
            {
            new { role = "user", content = userPrompt }
        }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json")
        };

        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        var httpResponse = await _http.SendAsync(request, ct);

        // Log error body before throwing so we can see exactly what Claude says
        if (!httpResponse.IsSuccessStatusCode)
        {
            var errorBody = await httpResponse.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "Claude API error. Status={Status} Body={Body}",
                httpResponse.StatusCode, errorBody);
            httpResponse.EnsureSuccessStatusCode();
        }

        var result = await httpResponse.Content
            .ReadFromJsonAsync<ClaudeResponse>(cancellationToken: ct);

        return result?.Content?.FirstOrDefault()?.Text ?? "";
    }

    // ── Prompt builders ───────────────────────────────────

    private static string BuildMorningPrompt(MorningForecastContext ctx)
    {
        var sb = new StringBuilder();
        var p = ctx.Profile;

        sb.AppendLine($"User: {p.FirstName}");
        sb.AppendLine($"Addictions: {string.Join(", ", p.AddictionTypes)}");
        sb.AppendLine($"Core motivation (sacred): {p.CoreMotivation}");
        sb.AppendLine($"Pre-urge feeling from setup: {p.PreUrgeDescription}");
        sb.AppendLine($"Risk windows: {string.Join(", ", p.RiskWindows)}");
        sb.AppendLine($"Habit duration: {p.HabitDuration}");

        if (p.IdentityFraming is not null)
            sb.AppendLine($"Identity framing: {p.IdentityFraming}");

        if (p.FutureSelfText is not null)
            sb.AppendLine($"Future self they described: {p.FutureSelfText}");

        sb.AppendLine();
        sb.AppendLine("This morning:");
        sb.AppendLine($"Mood: {ctx.MoodState}");
        sb.AppendLine($"Sleep: {ctx.SleepQuality}");
        sb.AppendLine($"Day structure: {ctx.DayStructure}");

        if (ctx.EmotionalCarry is not null)
            sb.AppendLine($"Emotional carry from yesterday: {ctx.EmotionalCarry}");

        if (ctx.TomorrowRisk is not null)
            sb.AppendLine($"They said last night: {ctx.TomorrowRisk}");

        if (ctx.YesterdayScore.HasValue)
            sb.AppendLine($"Yesterday score: {ctx.YesterdayScore}/10");

        sb.AppendLine();
        sb.AppendLine("Their answers today:");
        sb.AppendLine($"Q1: {ctx.Question1Text}");
        sb.AppendLine($"A1: {ctx.Answer1}");
        sb.AppendLine($"Q2: {ctx.Question2Text}");
        sb.AppendLine($"A2: {ctx.Answer2}");

        if (ctx.Summary?.Last7Days is not null)
        {
            sb.AppendLine();
            sb.AppendLine($"Last 7 days: {ctx.Summary.Last7Days}");
        }

        sb.AppendLine();
        sb.AppendLine("Write a forecast for today. 2 sentences maximum. 30 words maximum total.");
        sb.AppendLine("Do NOT ask questions. Make statements only. Predict what will happen today based on what they wrote.");

        if (ctx.TomorrowRisk is not null)
            sb.AppendLine("Reference what they said last night first.");
        if (ctx.EmotionalCarry is not null)
            sb.AppendLine("Reference the emotional state they are carrying, not just their schedule.");
        if (p.HabitDuration is "gt_3yr" or "1_3yr")
            sb.AppendLine("If referencing the habit, frame it as automatic not a character flaw.");

        sb.AppendLine("Reference what they actually wrote. Be specific. No fluff.");

        return sb.ToString();
    }

    private static string BuildWeeklyPrompt(WeeklyMirrorContext ctx)
    {
        var sb = new StringBuilder();
        var p = ctx.Profile;
        var s = ctx.Summary;

        sb.AppendLine($"User: {p.FirstName}");
        sb.AppendLine($"Addictions: {string.Join(", ", p.AddictionTypes)}");
        sb.AppendLine($"Core motivation: {p.CoreMotivation}");
        sb.AppendLine($"Habit duration: {p.HabitDuration}");

        if (p.FutureSelfText is not null)
            sb.AppendLine($"Future self they described: {p.FutureSelfText}");

        sb.AppendLine();
        sb.AppendLine("This week:");
        sb.AppendLine($"Urges: {s.TotalUrges}  Resisted: {s.TotalResisted}  Rate: {s.ResistRate:P0}");

        if (ctx.PreviousWeek is not null)
            sb.AppendLine($"vs last week: {ctx.PreviousWeek.ResistRate:P0} " +
                          $"(delta: {(s.ResistRate - ctx.PreviousWeek.ResistRate):+0.0%;-0.0%})");

        if (s.WorstDay is not null) sb.AppendLine($"Worst day: {s.WorstDay}");
        if (s.WorstHour.HasValue) sb.AppendLine($"Worst hour: {s.WorstHour}:00");
        if (s.TopHaltRoot is not null) sb.AppendLine($"Top HALT root: {s.TopHaltRoot}");

        if (ctx.PreUrgeContexts.Any())
        {
            sb.AppendLine();
            sb.AppendLine("Pre-urge context — exact quotes this week:");
            foreach (var q in ctx.PreUrgeContexts)
                sb.AppendLine($"- {q}");
        }

        sb.AppendLine();
        sb.AppendLine("Write 3 observations.");
        sb.AppendLine("For each observation include 1-2 exact quotes from pre-urge context that support it.");
        sb.AppendLine("Use their exact words — do not paraphrase quotes.");

        if (s.ResistRate > (ctx.PreviousWeek?.ResistRate ?? 0) && p.FutureSelfText is not null)
            sb.AppendLine("Resist rate improved — add one sentence referencing their future self description.");

        if (p.HabitDuration is "gt_3yr")
            sb.AppendLine("Frame patterns as automatic conditioning, not character flaws.");

        sb.AppendLine();
        sb.AppendLine("Return JSON only:");
        sb.AppendLine("[{\"observation\": \"text\", \"quotes\": [\"quote1\", \"quote2\"]}, ...]");
        sb.AppendLine("No other text. No markdown.");

        sb.AppendLine();
        sb.AppendLine("VOICE EXAMPLE — match this tone exactly:");
        sb.AppendLine("BAD: 'Your avoidance pattern suggests you may struggle this afternoon.'");
        sb.AppendLine("GOOD: 'The thing you're avoiding gets loud around 2pm. That is when it happens.'");
        sb.AppendLine("Write like the GOOD example. Short. Specific. Predictive. No explanation.");



        return sb.ToString();
    }

    // ── JSON parsing ──────────────────────────────────────

    private List<ObservationWithQuotes> ParseWeeklyObservations(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new List<ObservationWithQuotes>();

        try
        {
            // Strip markdown fences if present
            var json = raw
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            var items = JsonSerializer.Deserialize<List<WeeklyObservationJson>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return items?
                .Select(i => new ObservationWithQuotes(
                    Observation: i.Observation ?? "",
                    Quotes: i.Quotes ?? Array.Empty<string>()))
                .ToList()
                ?? new List<ObservationWithQuotes>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse weekly observations JSON.");
            return new List<ObservationWithQuotes>();
        }
    }

    // ── Response types ────────────────────────────────────

    private record ClaudeResponse(
        List<ClaudeContent>? Content);

    private record ClaudeContent(
        string? Text);

    private record WeeklyObservationJson(
        string? Observation,
        string[]? Quotes);
}