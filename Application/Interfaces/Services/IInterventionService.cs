using Frank.Application.DTOs.Responses;

namespace Frank.Application.Interfaces.Services;

/// <summary>
/// All context needed to select the right intervention.
/// Built by UrgeService and passed here.
/// </summary>
public record InterventionContext(
    Guid UserId,
    string AddictionType,
    int UrgeIntensity,
    string MoodValence,
    string? Environment,
    string? HaltRoot,
    string HabitDuration,
    string? IdentityFraming,
    bool SlippedToday,
    double ResistRateThisWeek,
    int HourOfDay
);

public interface IInterventionService
{
    /// <summary>
    /// Selects the appropriate intervention for this urge context.
    ///
    /// Decision flow:
    ///   1. HALT check — if halt_root found → return HALT intervention, stop
    ///   2. Intensity gate (from question_selection_rules):
    ///      8-10 → Frame 2 only (physical interrupt, no reflective questions)
    ///      4-7  → Frame 2 + light redirect
    ///      1-3  → Frame 1 possible (reflective question)
    ///   3. Scenario router — checks 12 signatures in priority order:
    ///      home_alone+evening+gaming+8-10 → U13 → U20
    ///      late_night+any+7-10            → U32 → U16
    ///      post_slip_today                → U27 → U08
    ///      social+cigarettes/weed         → U09 → U20
    ///      work/transit                   → U13 → U14 only
    ///      resist_rate under 30%          → urge surfing sequence
    ///      habit_duration gt_3yr+8-10     → U13 → U16 only
    ///      habit_duration lt_3mo+1-3      → U01 → U07
    ///      identity_framing=doer          → U15 → U08
    ///      DEFAULT                        → HALT H01-H05 → U01
    ///   4. Returns intervention with id, text, type, frame
    /// </summary>
    Task<InterventionResponse> SelectAsync(
        InterventionContext context,
        CancellationToken ct = default);
}