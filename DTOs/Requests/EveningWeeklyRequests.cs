using System.ComponentModel.DataAnnotations;

namespace Frank.Application.DTOs.Requests;

public record EveningCheckinRequest(

    /// <summary>stayed_in_control / slipped / hard_day</summary>
    [Required] string DayResult,

    /// <summary>Follow-up answers. Can be empty if user only tapped E01.</summary>
    AnswerItem[] Answers
);

public record WeeklyReflectionRequest(
    [Required] DateOnly WeekStart,
    [Required][MinLength(5)] string ReflectionText
);