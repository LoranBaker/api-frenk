namespace Frank.Domain.Entities;

/// <summary>
/// One row per user. Set during onboarding. Never delete.
/// 
/// SACRED FIELDS — PreUrgeDescription + CoreMotivation:
///   - Set once at onboarding
///   - Never editable by user after set (version history in PreUrgeVersions)
///   - Included in EVERY Claude call regardless of token pressure
///   - These two fields are the personalization foundation of the entire app
/// </summary>
public class UserProfile
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;

    // ── O01 — Addiction types ─────────────────────────────
    public string[] AddictionTypes { get; set; } = Array.Empty<string>();

    // ── O02 — Risk windows ───────────────────────────────
    public string[] RiskWindows { get; set; } = Array.Empty<string>();

    // ── O03 — SACRED ─────────────────────────────────────
    /// <summary>
    /// What it feels like right before giving in.
    /// Fed to every Claude prompt forever. Version history in PreUrgeVersions.
    /// </summary>
    public string PreUrgeDescription { get; set; } = string.Empty;

    // ── O04 — SACRED ─────────────────────────────────────
    /// <summary>
    /// The real reason they want to change. Their own words.
    /// Fed to every Claude prompt forever. Never editable.
    /// </summary>
    public string CoreMotivation { get; set; } = string.Empty;

    // ── O05 — Environment risks ───────────────────────────
    public string[] EnvironmentRisks { get; set; } = Array.Empty<string>();

    // ── O06 — Replacement stack ───────────────────────────
    /// <summary>
    /// 3 realistic alternatives user wrote themselves.
    /// Shown back during U20 intervention. Their words not the app's.
    /// </summary>
    public string[] ReplacementStack { get; set; } = Array.Empty<string>();

    // ── O16 — Variable E: Future self ────────────────────
    /// <summary>
    /// "Normal Tuesday 3 months from now."
    /// Referenced in weekly mirror on positive weeks only.
    /// </summary>
    public string? FutureSelfText { get; set; }

    // ── O17 — Variable A: Habit strength ─────────────────
    /// <summary>
    /// lt_3mo / 3_12mo / 1_3yr / gt_3yr
    /// Gates intervention tone and urge surfing sequence depth.
    /// Long duration = automatic behavior = physical interrupt priority.
    /// </summary>
    public string HabitDuration { get; set; } = string.Empty;

    // ── O18 — Variable Z: Identity framing ───────────────
    /// <summary>
    /// doer / trying_to_stop / unsure
    /// Gates identity mirror intervention weight.
    /// </summary>
    public string? IdentityFraming { get; set; }

    // ── Computed ──────────────────────────────────────────
    /// <summary>
    /// precontemplation / contemplation / preparation / action / maintenance
    /// Reassessed monthly. Never set by user directly.
    /// </summary>
    public string? ChangeStage { get; set; }

    /// <summary>
    /// Expo push token for FCM notifications.
    /// Null if user has not granted permission.
    /// </summary>
    public string? PushToken { get; set; }

    /// <summary>
    /// UTC offset in hours. e.g. 1 for UTC+1 (Bosnia).
    /// Used to send notifications at correct local time.
    /// </summary>
    public int TimezoneOffset { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ── Phase 2 fields ────────────────────────────────────
    public string? AddictionDuration { get; set; }   // O08
    public string? SocialSupportLevel { get; set; }   // O10
    public string? PostSlipEmotion { get; set; }   // O11
    public string? SpecificTriggers { get; set; }   // O13

    // ── Navigation ────────────────────────────────────────
    public ICollection<PreUrgeVersion> PreUrgeVersions { get; set; } = new List<PreUrgeVersion>();
    public ICollection<MorningCheckin> MorningCheckins { get; set; } = new List<MorningCheckin>();
    public ICollection<UrgeEvent> UrgeEvents { get; set; } = new List<UrgeEvent>();
    public ICollection<EveningCheckin> EveningCheckins { get; set; } = new List<EveningCheckin>();
    public ICollection<WeeklySummary> WeeklySummaries { get; set; } = new List<WeeklySummary>();
    public UserSummary? UserSummary { get; set; }
}