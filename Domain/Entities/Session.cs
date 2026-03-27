namespace Frank.Domain.Entities;

/// <summary>
/// Every question response from every session.
/// Replaces ALL individual question columns that existed before.
/// 
/// tap_value OR answer_text is populated — never both, never neither.
/// question_text is a snapshot — survives question rewording.
/// </summary>
public class Answer
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SessionId { get; set; }

    /// <summary>morning / urge / evening / weekly</summary>
    public string SessionType { get; set; } = string.Empty;

    /// <summary>FK to questions.id. e.g. M01</summary>
    public string QuestionId { get; set; } = string.Empty;

    /// <summary>
    /// Snapshot of question text at time of answer.
    /// Survives question rewording — history stays readable.
    /// </summary>
    public string QuestionText { get; set; } = string.Empty;

    /// <summary>Selected tap option value. NULL if free text answer.</summary>
    public string? TapValue { get; set; }

    /// <summary>Free text answer. NULL if tap option was selected.</summary>
    public string? AnswerText { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // ── Navigation ────────────────────────────────────────
    public ChatSession Session { get; set; } = null!;
}

/// <summary>
/// One row per session. Links all answers and events together.
/// Provides the session context for history view.
/// </summary>
public class ChatSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>morning / urge / evening / weekly</summary>
    public string SessionType { get; set; } = string.Empty;

    public DateOnly Date { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>NULL until session is completed.</summary>
    public DateTime? CompletedAt { get; set; }

    public bool Completed { get; set; } = false;

    // ── Navigation ────────────────────────────────────────
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
    public ICollection<ConversationHistory> Messages { get; set; } = new List<ConversationHistory>();
}

/// <summary>
/// Every message exchanged between Frank and the user.
/// Powers the chat history week view.
/// 
/// NEVER hard delete any row.
/// crisis_flag rows: never delete, never include in AI analysis.
/// deleted_at = soft delete only.
/// </summary>
public class ConversationHistory
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SessionId { get; set; }

    /// <summary>morning / urge / evening / weekly</summary>
    public string SessionType { get; set; } = string.Empty;

    public DateOnly Date { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>frank / user</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>Message text. NULL for tap_option rows.</summary>
    public string? Content { get; set; }

    /// <summary>text / tap_option / forecast / interrupt / result_badge / system</summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>Selected value for tap_option messages. NULL for text messages.</summary>
    public string? TapValue { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Soft delete timestamp. Never hard delete.</summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Set true when crisis keywords detected in any free text input.
    /// Never delete these rows. Never include in AI analysis.
    /// Triggers crisis protocol — normal flow stops.
    /// </summary>
    public bool CrisisFlag { get; set; } = false;

    // ── Navigation ────────────────────────────────────────
    public ChatSession Session { get; set; } = null!;
}