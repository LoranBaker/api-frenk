using System.ComponentModel.DataAnnotations;

namespace Frank.Application.DTOs.Requests;

public record HaltAnswer(
    [Required] string QuestionId,
    [Required] string TapValue
);

public record StartUrgeRequest(
    /// <summary>gaming / weed / cigarettes / gambling / social_media</summary>
    [Required] string AddictionType,

    /// <summary>1-10. Gates frame selection in InterventionService.</summary>
    [Required][Range(1, 10)] int UrgeIntensity,

    /// <summary>positive / neutral / negative</summary>
    [Required] string MoodValence,

    /// <summary>Emotion tap value e.g. avoiding_something, bored_loud</summary>
    [Required] string EmotionTap,

    /// <summary>home_alone / social / work / transit</summary>
    string? Environment,

    /// <summary>
    /// Device timezone offset in whole hours from UTC.
    /// Send as: new Date().getTimezoneOffset() / -60
    /// e.g. Sarajevo CET = +1, CEST = +2
    /// Used to compute local hour for midnight_regret routing.
    /// Defaults to 0 (UTC) if not sent.
    /// </summary>
    int TimezoneOffsetHours,

    /// <summary>H01-H05 tap answers. Empty if user skipped HALT.</summary>
    HaltAnswer[] HaltAnswers
);

public record ResolveUrgeRequest(
    [Required] Guid UrgeEventId,
    [Required] bool Resisted,

    /// <summary>
    /// Pre-urge context tap value — collected after resolve.
    /// Frame 1 collection: user is calmer after session ends.
    /// Optional — not required to resolve.
    /// </summary>
    string? PreUrgeContextTap
);

public record PostUrgeMoodRequest(
    [Required] Guid UrgeEventId,

    /// <summary>better / same / worse</summary>
    [Required] string Mood,

    /// <summary>Optional one-tap "what was that about" answer.</summary>
    string? WhatWasThat
);