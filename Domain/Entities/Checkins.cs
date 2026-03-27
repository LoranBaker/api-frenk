namespace Frank.Domain.Entities;

/// <summary>
/// One row per user per day. Metadata only.
/// Actual answers live in the answers table.
/// One morning check-in per calendar day maximum.
/// </summary>
public class MorningCheckin
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }

    /// <summary>
    /// Computed flag: unstructured + bad sleep + heavy mood = true.
    /// Adjusts Claude forecast tone when true.
    /// </summary>
    public bool HighRiskFlag { get; set; } = false;

    /// <summary>
    /// Claude-generated forecast. Max 30 words.
    /// NULL if Claude call failed — fallback shown to user instead.
    /// </summary>
    public string? ForecastText { get; set; }

    /// <summary>M03 answer. 1-10. Score under 5 = yesterday_hard=true.</summary>
    public int? YesterdayScore { get; set; }

    public Guid SessionId { get; set; }

    // ── Navigation ────────────────────────────────────────
    public UserProfile User { get; set; } = null!;
    public ChatSession Session { get; set; } = null!;
}

/// <summary>
/// One row per urge button press.
/// NEVER DELETE. This is the core outcome table.
/// resisted is the most important field in the entire database.
/// </summary>
public class UrgeEvent
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>gaming / weed / cigarettes / gambling / social_media</summary>
    public string AddictionType { get; set; } = string.Empty;

    /// <summary>1-10. Baseline before intervention. Gates frame selection.</summary>
    public int UrgeIntensity { get; set; }


    /// <summary>
    /// hungry / angry / lonely / tired / stressed.
    /// NULL if HALT checks came back clean.
    /// If set, HALT intervention was shown and CBT was skipped.
    /// </summary>
    public string? HaltRoot { get; set; }

    /// <summary>Which intervention was shown. e.g. U13, U01, U20.</summary>
    public string InterventionId { get; set; } = string.Empty;

    /// <summary>
    /// THE MOST IMPORTANT FIELD.
    /// Always set after resolve. Never null after POST /api/urge/resolve.
    /// </summary>
    public bool Resisted { get; set; }

    /// <summary>positive / neutral / negative</summary>
    public string MoodValence { get; set; } = string.Empty;

    /// <summary>home_alone / social / work / transit</summary>
    public string? Environment { get; set; }

    /// <summary>Auto-computed from timestamp. 0=Monday, 6=Sunday.</summary>
    public int DayOfWeek { get; set; }

    public Guid SessionId { get; set; }

    /// <summary>
    /// Collected via post-urge push notification 20-60 min after session.
    /// This is the Frame 1 pre_urge_context — collected when user is calm.
    /// better / same / worse
    /// </summary>
    public string? PostUrgeMood { get; set; }

    public string? PreUrgeContext { get; set; }


    // ── Navigation ────────────────────────────────────────
    public UserProfile User { get; set; } = null!;
    public ChatSession Session { get; set; } = null!;
}

/// <summary>
/// One row per user per day.
/// day_result is ALWAYS captured — it's the minimum viable evening check-in.
/// Actual follow-up answers live in the answers table.
/// </summary>
public class EveningCheckin
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }

    /// <summary>
    /// ALWAYS CAPTURED. stayed_in_control / slipped / hard_day.
    /// This is the one tap that is never optional.
    /// </summary>
    public string DayResult { get; set; } = string.Empty;

    /// <summary>Which addictions slipped. Only populated if day_result=slipped.</summary>
    public string[]? SlipType { get; set; }

    /// <summary>Auto-computed — did any urge_events exist today for this user.</summary>
    public bool UsedUrgeButton { get; set; }

    /// <summary>
    /// E14 answer. Feeds into tomorrow morning forecast as {tomorrow_risk}.
    /// Frank references it next morning: "Last night you said [words] — watch for that today."
    /// </summary>
    public string? TomorrowRisk { get; set; }

    /// <summary>E11. Where the hardest moment occurred. home_alone / social / work / transit</summary>
    public string? HardEnvironment { get; set; }

    public Guid SessionId { get; set; }

    // ── Navigation ────────────────────────────────────────
    public UserProfile User { get; set; } = null!;
    public ChatSession Session { get; set; } = null!;
}

/// <summary>
/// One row per Sunday per user.
/// Generated entirely by the Sunday 9pm cron job.
/// NEVER manually inserted.
/// </summary>
public class WeeklySummary
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Monday of that week.</summary>
    public DateOnly WeekStart { get; set; }

    public int TotalUrges { get; set; }
    public int TotalResisted { get; set; }
    public double ResistRate { get; set; }

    /// <summary>vs previous week. Positive = improving.</summary>
    public double? ResistRateDelta { get; set; }

    public string? WorstDay { get; set; }
    public int? WorstHour { get; set; }
    public string? TopHaltRoot { get; set; }

    // ── AI-generated observations ─────────────────────────
    public string? Observation1 { get; set; }
    public string? Observation2 { get; set; }
    public string? Observation3 { get; set; }

    /// <summary>
    /// JSON: [{obs_index: 0, quotes: ["quote1", "quote2"]}, ...]
    /// Raw user quotes supporting each observation.
    /// Frontend shows quotes BEFORE observation — pattern undeniable.
    /// </summary>
    public string? QuotesJson { get; set; }

    /// <summary>e.g. W01. Rotates weekly. Never repeats within 4 weeks.</summary>
    public string? ReflectionQId { get; set; }

    /// <summary>User's reflection answer. Becomes long-term journal entry.</summary>
    public string? ReflectionText { get; set; }

    // ── Navigation ────────────────────────────────────────
    public UserProfile User { get; set; } = null!;
}

/// <summary>
/// One row per user. Recomputed nightly at 3am by cron.
/// This is what gets fed to Claude — never raw event rows.
/// Keeps Claude context under 600 tokens regardless of user tenure.
/// </summary>
public class UserSummary
{
    public Guid UserId { get; set; }

    /// <summary>
    /// JSONB: {total_urges, resist_rate, worst_hour, top_halt, top_addiction}
    /// Aggregated from last 7 days of urge_events.
    /// </summary>
    public string? Last7Days { get; set; }

    /// <summary>
    /// JSONB: monthly trend — resist_rate trend, most_improved, biggest_challenge
    /// </summary>
    public string? Last30Days { get; set; }

    /// <summary>
    /// JSONB: milestones — days_active, total_resists, longest_streak
    /// </summary>
    public string? AllTime { get; set; }

    /// <summary>
    /// Today's urge press count. Recomputed on every press.
    /// Powers the daily counter notification UI.
    /// </summary>
    public int DailyUrgeCount { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation ────────────────────────────────────────
    public UserProfile User { get; set; } = null!;
}

/// <summary>
/// Version history for the sacred PreUrgeDescription field.
/// 
/// The language a user uses to describe their pre-urge state changes over time.
/// That change in language is itself recovery data — more precise = more self-aware.
/// Claude always uses the latest version. All versions preserved.
/// </summary>
public class PreUrgeVersion
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string VersionText { get; set; } = string.Empty;

    /// <summary>1 = original from onboarding. Increments on each edit.</summary>
    public int VersionNum { get; set; }

    /// <summary>onboarding / user_edit / prompted_review</summary>
    public string Source { get; set; } = "onboarding";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation ────────────────────────────────────────
    public UserProfile User { get; set; } = null!;
}

/// <summary>
/// Tracks sent notifications. Prevents duplicates.
/// Max 2 notifications per user per day enforced here.
/// </summary>
public class NotificationLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>morning_nudge / evening_gentle / post_urge_mood / milestone / mini_mirror</summary>
    public string Type { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateOnly Date { get; set; }

    /// <summary>Did user open the app from this notification.</summary>
    public bool Opened { get; set; } = false;

    /// <summary>urge_event_id or week_start for context. NULL for morning/evening nudges.</summary>
    public string? LinkedId { get; set; }

    // ── Navigation ────────────────────────────────────────
    public UserProfile User { get; set; } = null!;
}

/// <summary>
/// Tracks the day 3-7 mini mirror shown to new users.
/// Triggered when: day 3-7 after signup AND urge_count >= 3.
/// Shows user their own words from first urge sessions.
/// First moment user thinks "this app already knows my pattern."
/// </summary>
public class MiniMirrorEvent
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
    public int UrgeCount { get; set; }

    /// <summary>JSON array of pre_urge_context quotes shown to user.</summary>
    public string QuotesShown { get; set; } = "[]";

    /// <summary>Single Claude-generated observation from the quotes.</summary>
    public string Observation { get; set; } = string.Empty;

    public bool Opened { get; set; } = false;

    // ── Navigation ────────────────────────────────────────
    public UserProfile User { get; set; } = null!;
}