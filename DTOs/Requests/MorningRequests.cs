using System.ComponentModel.DataAnnotations;

namespace Frank.Application.DTOs.Requests;

/// <summary>
/// A single answer to a question.
/// tap_value OR answer_text must be present — never both null.
/// question_text is a snapshot saved alongside the answer
/// so history remains readable even if question wording changes.
/// </summary>
public record AnswerItem(
    [Required] string QuestionId,
    [Required] string QuestionText,
    string? TapValue,
    string? TapDisplayText,
    string? AnswerText
);

public record MorningCheckinRequest(
    Guid? SessionId,
    [Required][MinLength(2)] AnswerItem[] Answers
);