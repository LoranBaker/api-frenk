using System.ComponentModel.DataAnnotations;

namespace Frank.Application.DTOs.Requests;

public record OnboardingRequest(
    [Required][MinLength(1)] string[] AddictionTypes,
    [Required][MinLength(1)] string[] RiskWindows,

    /// <summary>
    /// O03 — SACRED. Describe what it feels like right before giving in.
    /// Stored in pre_urge_versions. Fed to every Claude prompt forever.
    /// </summary>
    [Required][MinLength(10)] string PreUrgeDescription,

    /// <summary>
    /// O04 — SACRED. The real reason they want to change.
    /// Their exact words. Never editable after set.
    /// </summary>
    [Required][MinLength(10)] string CoreMotivation,

    string[] EnvironmentRisks,

    /// <summary>O06 — 3 realistic replacements in user's own words.</summary>
    [Required][MinLength(1)][MaxLength(3)] string[] ReplacementStack,

    /// <summary>O17 — lt_3mo / 3_12mo / 1_3yr / gt_3yr</summary>
    [Required] string HabitDuration,

    /// <summary>O18 — doer / trying_to_stop / unsure</summary>
    string? IdentityFraming,

    /// <summary>O16 — Normal Tuesday description. Variable E.</summary>
    string? FutureSelfText,

    [Required][MaxLength(50)] string FirstName
);