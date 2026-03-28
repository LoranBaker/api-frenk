namespace Frank.Application.DTOs.Responses;

public record TapOptionResponse(
    Guid Id,
    string Text,
    string Value,
    int Order,
    string? PatternTag,
    string? ColorHint
);

public record QuestionResponse(
    string Id,
    string Text,
    string InputType,
    string? Frame,
    TapOptionResponse[] TapOptions
);

public record QuestionsResponse(
    QuestionResponse[] Questions
);

public record TapOptionMapItem(string Value, string Text);
public record TapOptionsMapResponse(TapOptionMapItem[] Options);



public record UserProfileResponse(
    Guid UserId,
    string FirstName,
    string[] AddictionTypes,
    string[] RiskWindows,
    string[] ReplacementStack,
    string HabitDuration,
    string? IdentityFraming,
    string? FutureSelfText,
    string? ChangeStage,
    DateTime CreatedAt
);