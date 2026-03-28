using System.ComponentModel.DataAnnotations.Schema;

namespace Frank.Domain.Entities;

/// <summary>
/// All questions live in this table. Zero hardcoded in frontend or backend code.
/// 
/// Add question  = INSERT row, set active=true
/// Remove question = UPDATE active=false
/// Change wording  = UPDATE text
/// 
/// None of the above require a code change or deployment.
/// </summary>
public class Question
{
    /// <summary>e.g. M01, U01, E01, H01. Unique across all session types.</summary>
    public string Id { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    /// <summary>morning / urge / evening / weekly / onboarding / halt</summary>
    public string SessionType { get; set; } = string.Empty;

    /// <summary>tap_only / tap_text / text_only</summary>
    public string InputType { get; set; } = string.Empty;

    /// <summary>mvp / phase2 / phase3</summary>
    public string Phase { get; set; } = "mvp";

    /// <summary>false = never shown. Toggle without deployment.</summary>
    public bool Active { get; set; } = true;

    /// <summary>Selection priority. Higher weight = shown more often in rotation.</summary>
    public int Weight { get; set; } = 1;

    /// <summary>
    /// JSON conditions for when to show.
    /// e.g. {"min_day": 3, "stage": "contemplation", "intensity_max": 7}
    /// </summary>
    public string? Conditions { get; set; }

    /// <summary>
    /// frame1       = emotional/reflective — morning slot 1, urge at intensity 1-3
    /// circumstance = situational — morning slot 2, evening
    /// frame2       = activation interrupt — urge at intensity 4-10 only
    /// </summary>
    public string? Frame { get; set; }

    /// <summary>
    /// Clinical patterns this question detects.
    /// InterventionService reads these to route based on pattern not raw answer.
    /// </summary>
    public string[] PatternTags { get; set; } = Array.Empty<string>();

    [Column("subtype")]
    public string? Subtype { get; set; }


    // ── Navigation ────────────────────────────────────────
    public ICollection<TapOption> TapOptions { get; set; } = new List<TapOption>();
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
    public ICollection<QuestionUsage> QuestionUsages { get; set; } = new List<QuestionUsage>();
}

/// <summary>
/// Pre-written honest answer options for a question.
/// 
/// Add option  = INSERT row
/// Remove option = UPDATE active=false
/// Change text   = UPDATE text
/// 
/// pattern_tag is what the algorithm reads — not the raw value.
/// </summary>
public class TapOption
{
    public Guid Id { get; set; }
    public string QuestionId { get; set; } = string.Empty;

    /// <summary>What the user sees. Human, honest, raw. Not clinical.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// What gets stored in answers.tap_value.
    /// snake_case. e.g. tired_behind, avoiding_something, actually_good
    /// </summary>
    public string Value { get; set; } = string.Empty;

    public int Order { get; set; }
    public bool Active { get; set; } = true;

    /// <summary>
    /// Clinical pattern this option detects.
    /// InterventionService reads this — never the raw value.
    /// e.g. avoidance, emotional_carry, sleep_deficit, isolation_risk
    /// </summary>
    public string? PatternTag { get; set; }

    /// <summary>warning / danger / success — frontend tints the pill accordingly.</summary>
    public string? ColorHint { get; set; }

    // ── Navigation ────────────────────────────────────────
    public Question Question { get; set; } = null!;
}

/// <summary>
/// Defines which frame of questions QuestionService picks per session.
/// 
/// This replaces hardcoded arrays like Frame1EmotionalQuestions = { "M02", "M09"... }.
/// Add a new morning slot = INSERT one row. Zero code change.
/// </summary>
public class QuestionSelectionRule
{
    public Guid Id { get; set; }

    /// <summary>morning / urge / evening / weekly</summary>
    public string SessionType { get; set; } = string.Empty;

    /// <summary>Human readable. e.g. morning_slot_1_emotional</summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>frame1 / circumstance / frame2</summary>
    public string Frame { get; set; } = string.Empty;

    /// <summary>Position in question set. 1 = first question shown, 2 = second.</summary>
    public int Slot { get; set; }

    /// <summary>true = always include one question of this frame per session.</summary>
    public bool Required { get; set; } = true;

    /// <summary>
    /// Optional constraints.
    /// e.g. {"intensity_min": 4} or {"intensity_max": 3} or {"min_day": 3}
    /// </summary>
    public string? Conditions { get; set; }

    public bool Active { get; set; } = true;
}

/// <summary>
/// Tracks which questions have been shown to each user.
/// Powers the 7-day cooldown — QuestionService excludes recently shown questions.
/// </summary>
public class QuestionUsage
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string QuestionId { get; set; } = string.Empty;
    public DateTime ShownAt { get; set; } = DateTime.UtcNow;
    public string SessionType { get; set; } = string.Empty;

    // ── Navigation ────────────────────────────────────────
    public Question Question { get; set; } = null!;
}

/// <summary>
/// All user-facing strings that may change without deployment.
/// 
/// Nothing user-facing is hardcoded in backend code.
/// Crisis response, keywords, counter messages, evening strings,
/// fallbacks, notification texts — ALL live here.
/// 
/// Update = change one DB row. Live immediately. Zero deployment.
/// </summary>
public class AppSetting
{
    /// <summary>
    /// Unique key. Examples:
    /// crisis_response, crisis_keywords, counter_message_2,
    /// evening_response_stayed, morning_forecast_fallback
    /// </summary>
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    /// <summary>Admin reference only. Never shown to user.</summary>
    public string? Description { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}